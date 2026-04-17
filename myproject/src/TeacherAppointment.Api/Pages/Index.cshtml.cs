using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherAppointment.Api.Pages;

public sealed class IndexModel : PortalPageModel
{
    public DateTimeOffset ServerTimeLocal { get; private set; }

    public void OnGet()
    {
        ServerTimeLocal = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8));
    }
}
