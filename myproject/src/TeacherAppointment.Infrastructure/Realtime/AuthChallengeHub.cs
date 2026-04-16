using Microsoft.AspNetCore.SignalR;

namespace TeacherAppointment.Infrastructure.Realtime;

public sealed class AuthChallengeHub : Hub
{
    public const string DesktopGroupPrefix = "desktop:";
    public const string MobileGroupPrefix = "mobile:";

    public Task JoinDesktopSessionAsync(string sessionId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"{DesktopGroupPrefix}{sessionId}");
    }

    public Task JoinMobileSessionAsync(string sessionId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"{MobileGroupPrefix}{sessionId}");
    }
}
