using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherAppointment.Api.Pages;

public abstract class PortalPageModel : PageModel
{
    [TempData]
    public string? FlashSuccess { get; set; }

    [TempData]
    public string? FlashWarning { get; set; }

    [TempData]
    public string? FlashError { get; set; }

    public bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin => string.Equals(User.FindFirstValue(ClaimTypes.Role), "admin", StringComparison.OrdinalIgnoreCase);

    public string UserDisplayName => User.FindFirstValue("name") ?? string.Empty;

    public string UserEmployeeNo => User.FindFirstValue("empl_no") ?? string.Empty;

    public string UserMaskedId => User.FindFirstValue("id_no_masked") ?? string.Empty;

    protected IActionResult RequireAuthenticated(string message = "請先登入再繼續。")
    {
        if (IsAuthenticated)
        {
            return Page();
        }

        FlashWarning = message;
        return RedirectToPage("/Auth/Login");
    }

    protected IActionResult RequireAdmin(string deniedMessage = "此頁面僅供管理員使用。")
    {
        if (IsAuthenticated && IsAdmin)
        {
            return Page();
        }

        FlashError = deniedMessage;
        return RedirectToPage("/Index");
    }
}
