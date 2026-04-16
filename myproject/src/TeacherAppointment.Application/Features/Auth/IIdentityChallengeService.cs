namespace TeacherAppointment.Application.Features.Auth;

public interface IIdentityChallengeService
{
    Task<ChallengeInitResult> InitializeAsync(string idNo, DateOnly birthday, CancellationToken cancellationToken = default);

    Task<EmailVerificationResult> VerifyEmailCodeAsync(string challengeId, string code, CancellationToken cancellationToken = default);

    Task<QrChallengeSessionResult> CreateQrSessionAsync(string challengeId, CancellationToken cancellationToken = default);

    Task<QrChallengeConfirmationResult> ConfirmQrSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}

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
