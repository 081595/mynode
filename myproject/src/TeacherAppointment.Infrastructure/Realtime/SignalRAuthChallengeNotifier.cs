using Microsoft.AspNetCore.SignalR;
using TeacherAppointment.Application.Abstractions.Realtime;

namespace TeacherAppointment.Infrastructure.Realtime;

public sealed class SignalRAuthChallengeNotifier : IAuthChallengeNotifier
{
    private readonly IHubContext<AuthChallengeHub> _hubContext;

    public SignalRAuthChallengeNotifier(IHubContext<AuthChallengeHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyDesktopRedirectAsync(string sessionId, string redirectUrl, CancellationToken cancellationToken = default)
    {
        return _hubContext
            .Clients
            .Group($"{AuthChallengeHub.DesktopGroupPrefix}{sessionId}")
            .SendAsync("desktop.redirect", new { SessionId = sessionId, RedirectUrl = redirectUrl }, cancellationToken);
    }

    public Task NotifyMobileCloseAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return _hubContext
            .Clients
            .Group($"{AuthChallengeHub.MobileGroupPrefix}{sessionId}")
            .SendAsync("mobile.close", new { SessionId = sessionId }, cancellationToken);
    }
}
