namespace TeacherAppointment.Application.Abstractions.Security;

public interface IRateLimitService
{
    RateLimitDecision Check(RateLimitRequest request);
}

public sealed record RateLimitRequest(
    string Action,
    string IdentityKey,
    string? ClientIp,
    DateTime TimestampUtc);

public sealed record RateLimitDecision(
    bool Allowed,
    TimeSpan? RetryAfter,
    string? Scope,
    string? Reason);
