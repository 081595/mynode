using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeacherAppointment.Application.Abstractions.Persistence;
using TeacherAppointment.Application.Features.AdminMaintenance;

namespace TeacherAppointment.Api.Pages.Admin;

public sealed class TeachersModel : PortalPageModel
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IAdminMaintenanceService _adminMaintenanceService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly TimeProvider _timeProvider;

    public TeachersModel(
        ITeacherRepository teacherRepository,
        IAdminMaintenanceService adminMaintenanceService,
        IAuditLogRepository auditLogRepository,
        TimeProvider timeProvider)
    {
        _teacherRepository = teacherRepository;
        _adminMaintenanceService = adminMaintenanceService;
        _auditLogRepository = auditLogRepository;
        _timeProvider = timeProvider;
    }

    [BindProperty(SupportsGet = true)]
    public int Year { get; set; } = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).Year - 1911;

    [BindProperty(SupportsGet = true)]
    public string? EmployeeNo { get; set; }

    [BindProperty]
    public TeacherUpsertInput Input { get; set; } = new();

    public IReadOnlyList<TeacherIdentityRecord> Rows { get; private set; } = [];

    public sealed class TeacherUpsertInput
    {
        public int Year { get; set; }
        public string EmployeeNo { get; set; } = string.Empty;
        public string IdNo { get; set; } = string.Empty;
        public DateOnly Birthday { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Role { get; set; } = "user";
        public bool IsActive { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await EnsureAdminAsync("admin.page.teachers", cancellationToken))
        {
            FlashError = "您沒有管理員權限。";
            return RedirectToPage("/Index");
        }

        Rows = await _teacherRepository.GetForAdminAsync(Year, EmployeeNo, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostUpsertAsync(CancellationToken cancellationToken)
    {
        if (!await EnsureAdminAsync("admin.page.teachers.upsert", cancellationToken))
        {
            return new JsonResult(new { success = false, message = "權限不足。", reload = true });
        }

        await _teacherRepository.UpsertForAdminAsync(
            new TeacherAdminUpsertInput(
                Input.Year,
                Input.EmployeeNo,
                Input.IdNo,
                Input.Birthday,
                Input.Name,
                Input.Email,
                Input.Role,
                Input.IsActive),
            cancellationToken);

        return new JsonResult(new { success = true, message = "教師資料已儲存。", reload = true });
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int year, string employeeNo, CancellationToken cancellationToken)
    {
        if (!await EnsureAdminAsync("admin.page.teachers.deactivate", cancellationToken))
        {
            return new JsonResult(new { success = false, message = "權限不足。" });
        }

        var deleted = await _teacherRepository.DeactivateAsync(year, employeeNo, cancellationToken);
        return new JsonResult(new { success = deleted, message = deleted ? "教師帳號已停用。" : "停用失敗。", reload = true });
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
