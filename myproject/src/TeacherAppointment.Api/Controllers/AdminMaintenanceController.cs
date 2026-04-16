using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherAppointment.Application.Abstractions.Persistence;
using TeacherAppointment.Application.Features.AdminMaintenance;

namespace TeacherAppointment.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public sealed class AdminMaintenanceController : ControllerBase
{
    private readonly IAdminMaintenanceService _adminMaintenanceService;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IAppointmentResponseRepository _appointmentRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly TimeProvider _timeProvider;

    public AdminMaintenanceController(
        IAdminMaintenanceService adminMaintenanceService,
        ITeacherRepository teacherRepository,
        IAppointmentResponseRepository appointmentRepository,
        IAuditLogRepository auditLogRepository,
        TimeProvider timeProvider)
    {
        _adminMaintenanceService = adminMaintenanceService;
        _teacherRepository = teacherRepository;
        _appointmentRepository = appointmentRepository;
        _auditLogRepository = auditLogRepository;
        _timeProvider = timeProvider;
    }

    [HttpGet("teachers")]
    public async Task<ActionResult<IReadOnlyList<TeacherIdentityRecord>>> GetTeachersAsync(
        [FromQuery] int year,
        [FromQuery] string? employeeNo,
        CancellationToken cancellationToken)
    {
        if (!await EnsureAdminAsync("admin.teacher.list", cancellationToken))
        {
            return Forbid();
        }

        var rows = await _teacherRepository.GetForAdminAsync(year, employeeNo, cancellationToken);
        return Ok(rows);
    }

    [HttpGet("teachers/{year:int}/{employeeNo}")]
    public async Task<ActionResult<TeacherIdentityRecord>> GetTeacherAsync(int year, string employeeNo, CancellationToken cancellationToken)
    {
        if (!await EnsureAdminAsync("admin.teacher.get", cancellationToken))
        {
            return Forbid();
        }

        var row = await _teacherRepository.GetByEmployeeNoAsync(year, employeeNo, cancellationToken);
        if (row is null)
        {
            return NotFound(new ErrorResponse("Teacher record not found."));
        }

        return Ok(row);
    }

    [HttpPut("teachers/{year:int}/{employeeNo}")]
    public async Task<ActionResult<TeacherIdentityRecord>> UpsertTeacherAsync(
        int year,
        string employeeNo,
        [FromBody] TeacherUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!await EnsureAdminAsync("admin.teacher.upsert", cancellationToken))
        {
            return Forbid();
        }

        var updated = await _teacherRepository.UpsertForAdminAsync(
            new TeacherAdminUpsertInput(
                year,
                employeeNo,
                request.IdNo,
                request.Birthday,
                request.Name,
                request.Email,
                request.Role,
                request.IsActive),
            cancellationToken);

        return Ok(updated);
    }

    [HttpDelete("teachers/{year:int}/{employeeNo}")]
    public async Task<ActionResult<DeleteTeacherResponse>> DeleteTeacherAsync(int year, string employeeNo, CancellationToken cancellationToken)
    {
        if (!await EnsureAdminAsync("admin.teacher.delete", cancellationToken))
        {
            return Forbid();
        }

        var deleted = await _teacherRepository.DeactivateAsync(year, employeeNo, cancellationToken);
        return Ok(new DeleteTeacherResponse(deleted));
    }

    [HttpGet("appointments")]
    public async Task<ActionResult<IReadOnlyList<AppointmentResponseSummary>>> GetAppointmentsAsync(
        [FromQuery] int year,
        [FromQuery] string? employeeNo,
        CancellationToken cancellationToken)
    {
        if (!await EnsureAdminAsync("admin.appointment.list", cancellationToken))
        {
            return Forbid();
        }

        var rows = await _appointmentRepository.GetForAdminAsync(year, employeeNo, cancellationToken);
        return Ok(rows);
    }

    [HttpPut("appointments/{year:int}/{employeeNo}/{docYear:int}/{docType}/{docSeq}")]
    public async Task<ActionResult<AppointmentAdminRecord>> UpsertAppointmentAsync(
        int year,
        string employeeNo,
        int docYear,
        string docType,
        string docSeq,
        [FromBody] AppointmentUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!await EnsureAdminAsync("admin.appointment.upsert", cancellationToken))
        {
            return Forbid();
        }

        byte[]? pdfBytes = null;
        if (!string.IsNullOrWhiteSpace(request.PdfBase64))
        {
            pdfBytes = Convert.FromBase64String(request.PdfBase64);
        }

        var key = new AppointmentDocumentKey(year, employeeNo, docYear, docType, docSeq);
        var updated = await _appointmentRepository.UpsertForAdminAsync(
            new AppointmentAdminUpsertInput(
                key,
                request.FileName,
                pdfBytes,
                request.ResponseStatus,
                request.DownloadCount,
                request.Remark),
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        return Ok(updated);
    }

    [HttpPatch("appointments/{year:int}/{employeeNo}/{docYear:int}/{docType}/{docSeq}/remark")]
    public async Task<ActionResult<AppointmentAdminRecord>> UpdateRemarkAsync(
        int year,
        string employeeNo,
        int docYear,
        string docType,
        string docSeq,
        [FromBody] UpdateRemarkRequest request,
        CancellationToken cancellationToken)
    {
        if (!await EnsureAdminAsync("admin.appointment.remark", cancellationToken))
        {
            return Forbid();
        }

        var key = new AppointmentDocumentKey(year, employeeNo, docYear, docType, docSeq);
        var updated = await _appointmentRepository.UpdateRemarkForAdminAsync(
            key,
            request.Remark,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        if (updated is null)
        {
            return NotFound(new ErrorResponse("Appointment record not found."));
        }

        return Ok(updated);
    }

    private async Task<bool> EnsureAdminAsync(string eventType, CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var isAdmin = await _adminMaintenanceService.IsAuthorizedAsync(role, cancellationToken);
        if (isAdmin)
        {
            return true;
        }

        await _auditLogRepository.WriteAsync(
            new AuditLogEntry(
                IdNo: User.FindFirstValue("id_no_masked"),
                VerifyMethod: VerifyMethod.None,
                TargetEmail: null,
                ClientIp: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserAgent: Request.Headers.UserAgent.ToString(),
                Success: false,
                FailureReason: "authorization_denied",
                TimestampUtc: _timeProvider.GetUtcNow().UtcDateTime,
                EventType: eventType,
                Metadata: new Dictionary<string, string?>
                {
                    ["path"] = HttpContext.Request.Path,
                    ["method"] = HttpContext.Request.Method,
                    ["role"] = role
                }),
            cancellationToken);

        return false;
    }

    public sealed record TeacherUpsertRequest(
        string IdNo,
        DateOnly Birthday,
        string Name,
        string? Email,
        string Role,
        bool IsActive);

    public sealed record DeleteTeacherResponse(bool Deleted);

    public sealed record AppointmentUpsertRequest(
        string? FileName,
        string? PdfBase64,
        int ResponseStatus,
        int DownloadCount,
        string? Remark);

    public sealed record UpdateRemarkRequest(string? Remark);

    public sealed record ErrorResponse(string Message);
}
