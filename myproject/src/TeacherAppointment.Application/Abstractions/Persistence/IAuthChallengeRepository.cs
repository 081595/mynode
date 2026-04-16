namespace TeacherAppointment.Application.Abstractions.Persistence;

public interface IAuthChallengeRepository
{
    Task<AuthChallengeRecord> SaveAsync(AuthChallengeRecord challenge, CancellationToken cancellationToken = default);

    Task<AuthChallengeRecord?> GetByIdAsync(string challengeId, CancellationToken cancellationToken = default);

    Task<AuthChallengeRecord?> GetByQrSessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
}

public sealed record AuthChallengeRecord(
    string ChallengeId,
    int Year,
    string EmployeeNo,
    string IdNo,
    string? TargetEmail,
    string VerificationCode,
    DateTime ExpiresAtUtc,
    DateTime ResendAvailableAtUtc,
    bool IsVerified,
    VerifyMethod VerifiedBy,
    DateTime? VerifiedAtUtc,
    string? QrSessionId,
    DateTime? QrSessionExpiresAtUtc,
    bool IsCompleted);

public enum VerifyMethod
{
    None = 0,
    Email = 1,
    QrCode = 2
}
