using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TeacherAppointment.Api.Security;
using TeacherAppointment.Application.Features.Auth;

namespace TeacherAppointment.Api.Pages.Auth;

public sealed class LogoutModel : PortalPageModel
{
    private readonly IAuthSessionService _authSessionService;
    private readonly AuthCookieOptions _cookieOptions;

    public LogoutModel(IAuthSessionService authSessionService, IOptions<AuthCookieOptions> cookieOptions)
    {
        _authSessionService = authSessionService;
        _cookieOptions = cookieOptions.Value;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(_cookieOptions.RefreshTokenName, out var refreshToken);
        await _authSessionService.LogoutAsync(
            refreshToken,
            new AuthClientContext(
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Request.Headers.UserAgent.ToString()),
            cancellationToken);

        DeleteAuthCookies();
        FlashSuccess = "您已登出。";
        return RedirectToPage("/Index");
    }

    private void DeleteAuthCookies()
    {
        var sameSite = Enum.TryParse<SameSiteMode>(_cookieOptions.SameSite, ignoreCase: true, out var parsed)
            ? parsed
            : SameSiteMode.Strict;

        var deleteOptions = new CookieOptions
        {
            HttpOnly = _cookieOptions.HttpOnly,
            Secure = _cookieOptions.Secure,
            SameSite = sameSite,
            Path = "/"
        };

        Response.Cookies.Delete(_cookieOptions.AccessTokenName, deleteOptions);
        Response.Cookies.Delete(_cookieOptions.RefreshTokenName, deleteOptions);
    }
}
