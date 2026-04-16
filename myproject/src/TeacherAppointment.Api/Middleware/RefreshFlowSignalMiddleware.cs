using TeacherAppointment.Api.Security;
using Microsoft.Extensions.Options;

namespace TeacherAppointment.Api.Middleware;

public sealed class RefreshFlowSignalMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthCookieOptions _cookieOptions;

    public RefreshFlowSignalMiddleware(RequestDelegate next, IOptions<AuthCookieOptions> cookieOptions)
    {
        _next = next;
        _cookieOptions = cookieOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status401Unauthorized &&
            context.Request.Cookies.ContainsKey(_cookieOptions.RefreshTokenName))
        {
            context.Response.Headers.TryAdd("X-Refresh-Flow", "required");
        }
    }
}
