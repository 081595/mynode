using System.Security.Cryptography;
using TeacherAppointment.Application.Abstractions.Persistence;
using TeacherAppointment.Application.Abstractions.Security;

namespace TeacherAppointment.Application.Features.Auth;

public sealed class AuthSessionService : IAuthSessionService
{
    private readonly IAuthChallengeRepository _challengeRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenFactory _tokenFactory;
    private readonly TimeProvider _timeProvider;

    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public AuthSessionService(
        IAuthChallengeRepository challengeRepository,
        ITeacherRepository teacherRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenFactory tokenFactory,
        TimeProvider timeProvider)
    {
        _challengeRepository = challengeRepository;
        _teacherRepository = teacherRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenFactory = tokenFactory;
        _timeProvider = timeProvider;
    }

    public async Task<SessionIssueResult> IssueTokensAsync(string challengeId, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var challenge = await _challengeRepository.GetByIdAsync(challengeId, cancellationToken);
        if (challenge is null || !challenge.IsVerified)
        {
            return new SessionIssueResult(false, "Challenge is not verified.", null, null, null, null, null);
        }

        var teacher = await _teacherRepository.GetByEmployeeNoAsync(challenge.Year, challenge.EmployeeNo, cancellationToken);
        if (teacher is null || !teacher.IsActive)
        {
            return new SessionIssueResult(false, "User account not found.", null, null, null, null, null);
        }

        var accessExpiresAtUtc = now.Add(AccessTokenLifetime);
        var refreshExpiresAtUtc = now.Add(RefreshTokenLifetime);
        var accessToken = _tokenFactory.CreateAccessToken(teacher, now, accessExpiresAtUtc);
        var refreshToken = GenerateRefreshToken();

        await _refreshTokenRepository.SaveAsync(
            new RefreshTokenRecord(refreshToken, teacher.Year, teacher.EmployeeNo, now, refreshExpiresAtUtc, null),
            cancellationToken);

        return new SessionIssueResult(
            true,
            "Tokens issued.",
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            refreshToken,
            refreshExpiresAtUtc,
            teacher.EmployeeNo);
    }

    public async Task<SessionRefreshResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var activeToken = await _refreshTokenRepository.GetActiveAsync(refreshToken, now, cancellationToken);
        if (activeToken is null)
        {
            return new SessionRefreshResult(false, "Refresh token is invalid.", null, null, null, null, null);
        }

        var teacher = await _teacherRepository.GetByEmployeeNoAsync(activeToken.Year, activeToken.EmployeeNo, cancellationToken);
        if (teacher is null || !teacher.IsActive)
        {
            return new SessionRefreshResult(false, "User account not found.", null, null, null, null, null);
        }

        var accessExpiresAtUtc = now.Add(AccessTokenLifetime);
        var refreshExpiresAtUtc = now.Add(RefreshTokenLifetime);
        var accessToken = _tokenFactory.CreateAccessToken(teacher, now, accessExpiresAtUtc);
        var rotatedRefreshToken = GenerateRefreshToken();

        await _refreshTokenRepository.RevokeAsync(refreshToken, now, cancellationToken);
        await _refreshTokenRepository.SaveAsync(
            new RefreshTokenRecord(rotatedRefreshToken, teacher.Year, teacher.EmployeeNo, now, refreshExpiresAtUtc, null),
            cancellationToken);

        return new SessionRefreshResult(
            true,
            "Token refreshed.",
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            rotatedRefreshToken,
            refreshExpiresAtUtc,
            teacher.EmployeeNo);
    }

    public async Task<SessionLogoutResult> LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var activeToken = await _refreshTokenRepository.GetActiveAsync(refreshToken, now, cancellationToken);
            if (activeToken is not null)
            {
                await _refreshTokenRepository.RevokeByUserAsync(activeToken.Year, activeToken.EmployeeNo, now, cancellationToken);
            }
            else
            {
                await _refreshTokenRepository.RevokeAsync(refreshToken, now, cancellationToken);
            }
        }

        return new SessionLogoutResult(true, "Logged out.");
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(bytes);
    }
}
