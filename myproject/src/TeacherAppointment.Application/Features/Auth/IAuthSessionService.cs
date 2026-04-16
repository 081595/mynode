namespace TeacherAppointment.Application.Features.Auth;

public interface IAuthSessionService
{
    Task<SessionIssueResult> IssueTokensAsync(string challengeId, AuthClientContext clientContext, CancellationToken cancellationToken = default);

    Task<SessionRefreshResult> RefreshAsync(string refreshToken, AuthClientContext clientContext, CancellationToken cancellationToken = default);

    Task<SessionLogoutResult> LogoutAsync(string? refreshToken, AuthClientContext clientContext, CancellationToken cancellationToken = default);
}

public sealed record SessionIssueResult(
    bool Success,
    string Message,
    string? AccessToken,
    DateTime? AccessTokenExpiresAtUtc,
    string? RefreshToken,
    DateTime? RefreshTokenExpiresAtUtc,
    string? EmployeeNo);

public sealed record SessionRefreshResult(
    bool Success,
    string Message,
    string? AccessToken,
    DateTime? AccessTokenExpiresAtUtc,
    string? RefreshToken,
    DateTime? RefreshTokenExpiresAtUtc,
    string? EmployeeNo);

public sealed record SessionLogoutResult(bool Success, string Message);
