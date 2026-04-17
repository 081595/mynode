using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeacherAppointment.Application.Abstractions.Persistence;
using TeacherAppointment.Application.Features.AdminMaintenance;

namespace TeacherAppointment.Api.Pages.Admin;

public sealed class AppointmentsModel : PortalPageModel
{
    private readonly IAppointmentResponseRepository _appointmentRepository;
    private readonly IAdminMaintenanceService _adminMaintenanceService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly TimeProvider _timeProvider;

    public AppointmentsModel(
        IAppointmentResponseRepository appointmentRepository,
        IAdminMaintenanceService adminMaintenanceService,
        IAuditLogRepository auditLogRepository,
        TimeProvider timeProvider)
    {
        _appointmentRepository = appointmentRepository;
        _adminMaintenanceService = adminMaintenanceService;
        _auditLogRepository = auditLogRepository;
        _timeProvider = timeProvider;
    }

    [BindProperty(SupportsGet = true)]
    public int Year { get; set; } = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).Year - 1911;

    [BindProperty(SupportsGet = true)]
    public string? EmployeeNo { get; set; }

    [BindProperty]
    public RemarkInput Input { get; set; } = new();

    [BindProperty]
    public UploadInput Upload { get; set; } = new();

    public IReadOnlyList<AppointmentResponseSummary> Rows { get; private set; } = [];

    public sealed class RemarkInput
    {
        public int Year { get; set; }
        public string EmployeeNo { get; set; } = string.Empty;
        public int DocYear { get; set; }
        public string DocType { get; set; } = string.Empty;
        public string DocSeq { get; set; } = string.Empty;
        public string? Remark { get; set; }
    }

    public sealed class UploadInput
    {
        public int Year { get; set; }
        public string EmployeeNo { get; set; } = string.Empty;
        public int DocYear { get; set; }
        public string DocType { get; set; } = string.Empty;
        public string DocSeq { get; set; } = string.Empty;
        public IFormFile? PdfFile { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await EnsureAdminAsync("admin.page.appointments", cancellationToken))
        {
            FlashError = "您沒有管理員權限。";
            return RedirectToPage("/Index");
        }

        Rows = await _appointmentRepository.GetForAdminAsync(Year, EmployeeNo, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateRemarkAsync(CancellationToken cancellationToken)
    {
        if (!await EnsureAdminAsync("admin.page.appointments.remark", cancellationToken))
        {
            return new JsonResult(new { success = false, message = "權限不足。" });
        }

        var key = new AppointmentDocumentKey(Input.Year, Input.EmployeeNo, Input.DocYear, Input.DocType, Input.DocSeq);
        var updated = await _appointmentRepository.UpdateRemarkForAdminAsync(
            key,
            Input.Remark,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        if (updated is null)
        {
            return new JsonResult(new { success = false, message = "找不到聘書紀錄。" });
        }

        return new JsonResult(new { success = true, message = "備註已更新。", reload = true });
    }

    public async Task<IActionResult> OnPostUploadPdfAsync(CancellationToken cancellationToken)
    {
        if (!await EnsureAdminAsync("admin.page.appointments.upload", cancellationToken))
        {
            return new JsonResult(new { success = false, message = "權限不足。" });
        }

        if (Upload.PdfFile is null || Upload.PdfFile.Length == 0)
        {
            return new JsonResult(new { success = false, message = "請選擇 PDF 檔案。" });
        }

        if (!string.Equals(Path.GetExtension(Upload.PdfFile.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return new JsonResult(new { success = false, message = "僅接受 PDF 格式。" });
        }

        await using var stream = Upload.PdfFile.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);

        var key = new AppointmentDocumentKey(Upload.Year, Upload.EmployeeNo, Upload.DocYear, Upload.DocType, Upload.DocSeq);
        await _appointmentRepository.UpsertForAdminAsync(
            new AppointmentAdminUpsertInput(
                key,
                Upload.PdfFile.FileName,
                memory.ToArray(),
                0,
                0,
                null),
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        return new JsonResult(new { success = true, message = "PDF 已上傳。", reload = true });
    }

    private async Task<bool> EnsureAdminAsync(string eventType, CancellationToken cancellationToken)
    {
        if (IsAuthenticated && await _adminMaintenanceService.IsAuthorizedAsync(User.FindFirstValue(System.Security.Claims.ClaimTypes.Role) ?? string.Empty, cancellationToken))
        {
            return true;
        }

        await _auditLogRepository.WriteAsync(
            new AuditLogEntry(
                UserMaskedId,
                VerifyMethod.None,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Request.Headers.UserAgent.ToString(),
                false,
                "authorization_denied",
                _timeProvider.GetUtcNow().UtcDateTime,
                eventType,
                new Dictionary<string, string?>
                {
                    ["path"] = HttpContext.Request.Path,
                    ["method"] = HttpContext.Request.Method
                }),
            cancellationToken);

        return false;
    }
}
