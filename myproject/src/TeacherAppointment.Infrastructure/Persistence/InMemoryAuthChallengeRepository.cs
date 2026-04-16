using System.Collections.Concurrent;
using TeacherAppointment.Application.Abstractions.Persistence;

namespace TeacherAppointment.Infrastructure.Persistence;

public sealed class InMemoryAuthChallengeRepository : IAuthChallengeRepository
{
    private readonly ConcurrentDictionary<string, AuthChallengeRecord> _challenges = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _challengeByQrSession = new(StringComparer.Ordinal);

    public Task<AuthChallengeRecord?> GetByIdAsync(string challengeId, CancellationToken cancellationToken = default)
    {
        _challenges.TryGetValue(challengeId, out var challenge);
        return Task.FromResult<AuthChallengeRecord?>(challenge);
    }

    public Task<AuthChallengeRecord?> GetByQrSessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_challengeByQrSession.TryGetValue(sessionId, out var challengeId) &&
            _challenges.TryGetValue(challengeId, out var challenge))
        {
            return Task.FromResult<AuthChallengeRecord?>(challenge);
        }

        return Task.FromResult<AuthChallengeRecord?>(null);
    }

    public Task<AuthChallengeRecord> SaveAsync(AuthChallengeRecord challenge, CancellationToken cancellationToken = default)
    {
        _challenges[challenge.ChallengeId] = challenge;

        if (!string.IsNullOrWhiteSpace(challenge.QrSessionId))
        {
            _challengeByQrSession[challenge.QrSessionId] = challenge.ChallengeId;
        }

        return Task.FromResult(challenge);
    }
}
