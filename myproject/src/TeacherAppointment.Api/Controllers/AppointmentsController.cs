using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherAppointment.Application.Abstractions.Persistence;

namespace TeacherAppointment.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public sealed class AppointmentsController : ControllerBase
{
    private readonly IAppointmentResponseRepository _appointmentRepository;
    private readonly TimeProvider _timeProvider;

    public AppointmentsController(IAppointmentResponseRepository appointmentRepository, TimeProvider timeProvider)
    {
        _appointmentRepository = appointmentRepository;
        _timeProvider = timeProvider;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppointmentResponseSummary>>> ListAsync(
        [FromQuery] int year,
        [FromQuery] string? employeeNo,
        CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var currentEmployeeNo = User.FindFirstValue("empl_no") ?? string.Empty;

        if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            var rows = await _appointmentRepository.GetForAdminAsync(year, employeeNo, cancellationToken);
            return Ok(rows);
        }

        var rowsForTeacher = await _appointmentRepository.GetForTeacherAsync(year, currentEmployeeNo, cancellationToken);
        return Ok(rowsForTeacher);
    }

    [HttpGet("{year:int}/{employeeNo}/{docYear:int}/{docType}/{docSeq}/pdf")]
    public async Task<IActionResult> DownloadPdfAsync(
        int year,
        string employeeNo,
        int docYear,
        string docType,
        string docSeq,
        CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var currentEmployeeNo = User.FindFirstValue("empl_no") ?? string.Empty;
        var isAdmin = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);

        if (!isAdmin && !string.Equals(currentEmployeeNo, employeeNo, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var key = new AppointmentDocumentKey(year, employeeNo, docYear, docType, docSeq);
        var payload = await _appointmentRepository.GetPdfAsync(
            key,
            incrementDownloadCount: !isAdmin,
            nowUtc: _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken: cancellationToken);

        if (payload is null)
        {
            return NotFound(new ErrorResponse("Appointment document not found."));
        }

        return File(payload.PdfContent, "application/pdf", payload.FileName);
    }

    [HttpPost("{year:int}/{employeeNo}/{docYear:int}/{docType}/{docSeq}/complete")]
    public async Task<ActionResult<CompleteResponse>> CompleteResponseAsync(
        int year,
        string employeeNo,
        int docYear,
        string docType,
        string docSeq,
        CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var currentEmployeeNo = User.FindFirstValue("empl_no") ?? string.Empty;
        if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(currentEmployeeNo, employeeNo, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var key = new AppointmentDocumentKey(year, employeeNo, docYear, docType, docSeq);
        var result = await _appointmentRepository.MarkResponseCompletedAsync(
            key,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        if (!result.Found)
        {
            return NotFound(new ErrorResponse("Appointment record not found."));
        }

        return Ok(new CompleteResponse(result.IsCompleted, result.Changed, result.UpdatedAtUtc));
    }

    public sealed record CompleteResponse(bool IsCompleted, bool Changed, DateTime? UpdatedAtUtc);

    public sealed record ErrorResponse(string Message);
}
