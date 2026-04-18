using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherAppointment.Api.Pages.Auth;

public class ConfirmModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string SessionId { get; set; } = string.Empty;

    public void OnGet(string sessionId)
    {
        SessionId = sessionId;
    }
}
