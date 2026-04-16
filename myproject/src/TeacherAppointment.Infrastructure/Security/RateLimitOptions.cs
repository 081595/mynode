namespace TeacherAppointment.Infrastructure.Security;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public ActionLimit Identify { get; init; } = new(5, 60);

    public ActionLimit ChallengeResend { get; init; } = new(3, 300);
}

public sealed record ActionLimit(int MaxAttempts, int WindowSeconds)
{
    public TimeSpan Window => TimeSpan.FromSeconds(WindowSeconds <= 0 ? 1 : WindowSeconds);
}
