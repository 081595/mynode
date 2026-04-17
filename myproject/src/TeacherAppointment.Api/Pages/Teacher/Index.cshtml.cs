using Microsoft.AspNetCore.Mvc;
using TeacherAppointment.Application.Abstractions.Persistence;

namespace TeacherAppointment.Api.Pages.Teacher;

public sealed class IndexModel : PortalPageModel
{
    private readonly IAppointmentResponseRepository _appointmentRepository;
    private readonly TimeProvider _timeProvider;

    public IndexModel(IAppointmentResponseRepository appointmentRepository, TimeProvider timeProvider)
    {
        _appointmentRepository = appointmentRepository;
        _timeProvider = timeProvider;
    }

    [BindProperty(SupportsGet = true)]
    public int Year { get; set; } = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).Year - 1911;

    public IReadOnlyList<AppointmentResponseSummary> Rows { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!IsAuthenticated)
        {
            FlashWarning = "登入逾時，請重新驗證。";
            return RedirectToPage("/Auth/Login");
        }

        Rows = await _appointmentRepository.GetForTeacherAsync(Year, UserEmployeeNo, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteAsync(
        int year,
        string employeeNo,
        int docYear,
        string docType,
        string docSeq,
        CancellationToken cancellationToken)
    {
        if (!IsAuthenticated || !string.Equals(UserEmployeeNo, employeeNo, StringComparison.OrdinalIgnoreCase))
        {
            return new JsonResult(new { success = false, message = "登入逾時或權限不足。", redirect = Url.Page("/Auth/Login") });
        }

        var key = new AppointmentDocumentKey(year, employeeNo, docYear, docType, docSeq);
        var result = await _appointmentRepository.MarkResponseCompletedAsync(
            key,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        if (!result.Found)
        {
            return new JsonResult(new { success = false, message = "找不到聘書紀錄。" });
        }

        return new JsonResult(new
        {
            success = true,
            message = result.Changed ? "已完成回覆。" : "此聘書已是回覆狀態。",
            isCompleted = result.IsCompleted,
            updatedAtUtc = result.UpdatedAtUtc?.ToString("O")
        });
    }
}
