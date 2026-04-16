namespace TeacherAppointment.Application.Features.Auth;

public interface IIdentityChallengeService
{
    Task<ChallengeInitResult> InitializeAsync(string idNo, DateOnly birthday, CancellationToken cancellationToken = default);
}

public sealed record ChallengeInitResult(bool Success, string? Message);
