namespace TeacherAppointment.Application.Features.Auth;

public interface IIdentityChallengeService
{
    Task<ChallengeInitResult> InitializeAsync(string idNo, DateOnly birthday, AuthClientContext clientContext, CancellationToken cancellationToken = default);

    Task<ChallengeResendResult> ResendChallengeAsync(string challengeId, AuthClientContext clientContext, CancellationToken cancellationToken = default);

    Task<EmailVerificationResult> VerifyEmailCodeAsync(string challengeId, string code, AuthClientContext clientContext, CancellationToken cancellationToken = default);

    Task<QrChallengeSessionResult> CreateQrSessionAsync(string challengeId, AuthClientContext clientContext, CancellationToken cancellationToken = default);

    Task<QrChallengeConfirmationResult> ConfirmQrSessionAsync(string sessionId, AuthClientContext clientContext, CancellationToken cancellationToken = default);
}

public sealed record AuthClientContext(
    string ClientIp,
    string UserAgent);

public sealed record ChallengeInitResult(
    bool Success,
    string Message,
    string? ChallengeId,
    string? EmployeeNo,
    string? MaskedEmail,
    DateTime? ExpiresAtUtc,
    DateTime? ResendAvailableAtUtc);

public sealed record EmailVerificationResult(
    bool Success,
    string Message,
    string ChallengeId,
    bool IsVerified,
    DateTime? VerifiedAtUtc);

public sealed record ChallengeResendResult(
    bool Success,
    bool Throttled,
    string Message,
    string ChallengeId,
    DateTime? ExpiresAtUtc,
    DateTime? ResendAvailableAtUtc,
    TimeSpan? RetryAfter);

public sealed record QrChallengeSessionResult(
    bool Success,
    string Message,
    string? ChallengeId,
    string? SessionId,
    DateTime? ExpiresAtUtc);

public sealed record QrChallengeConfirmationResult(
    bool Success,
    string Message,
    string? ChallengeId,
    string? SessionId,
    bool IsVerified,
    DateTime? VerifiedAtUtc,
    string? RedirectUrl);
