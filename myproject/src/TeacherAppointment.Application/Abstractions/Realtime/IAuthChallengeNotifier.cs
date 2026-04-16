namespace TeacherAppointment.Application.Abstractions.Realtime;

public interface IAuthChallengeNotifier
{
    Task NotifyDesktopRedirectAsync(string sessionId, string redirectUrl, CancellationToken cancellationToken = default);

    Task NotifyMobileCloseAsync(string sessionId, CancellationToken cancellationToken = default);
}
