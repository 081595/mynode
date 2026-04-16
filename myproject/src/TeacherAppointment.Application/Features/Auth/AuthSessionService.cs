using System.Security.Cryptography;
using TeacherAppointment.Application.Abstractions.Persistence;
using TeacherAppointment.Application.Abstractions.Security;

namespace TeacherAppointment.Application.Features.Auth;

public sealed class AuthSessionService : IAuthSessionService
{
    private readonly IAuthChallengeRepository _challengeRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITokenFactory _tokenFactory;
    private readonly TimeProvider _timeProvider;

    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public AuthSessionService(
        IAuthChallengeRepository challengeRepository,
        ITeacherRepository teacherRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditLogRepository auditLogRepository,
        ITokenFactory tokenFactory,
        TimeProvider timeProvider)
    {
        _challengeRepository = challengeRepository;
        _teacherRepository = teacherRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _auditLogRepository = auditLogRepository;
        _tokenFactory = tokenFactory;
        _timeProvider = timeProvider;
    }

    public async Task<SessionIssueResult> IssueTokensAsync(string challengeId, AuthClientContext clientContext, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var challenge = await _challengeRepository.GetByIdAsync(challengeId, cancellationToken);
        if (challenge is null || !challenge.IsVerified)
        {
            await WriteAuditAsync(
                idNo: challenge?.IdNo,
                verifyMethod: challenge?.VerifiedBy ?? VerifyMethod.None,
                targetEmail: challenge?.TargetEmail,
                success: false,
                failureReason: "challenge_not_verified",
                timestampUtc: now,
                eventType: "session.exchange",
                clientContext,
                cancellationToken);

            return new SessionIssueResult(false, "Challenge is not verified.", null, null, null, null, null);
        }

        var teacher = await _teacherRepository.GetByEmployeeNoAsync(challenge.Year, challenge.EmployeeNo, cancellationToken);
        if (teacher is null || !teacher.IsActive)
        {
            await WriteAuditAsync(
                idNo: challenge.IdNo,
                verifyMethod: challenge.VerifiedBy,
                targetEmail: challenge.TargetEmail,
                success: false,
                failureReason: "account_not_active",
                timestampUtc: now,
                eventType: "session.exchange",
                clientContext,
                cancellationToken);

            return new SessionIssueResult(false, "User account not found.", null, null, null, null, null);
        }

        var accessExpiresAtUtc = now.Add(AccessTokenLifetime);
        var refreshExpiresAtUtc = now.Add(RefreshTokenLifetime);
        var accessToken = _tokenFactory.CreateAccessToken(teacher, now, accessExpiresAtUtc);
        var refreshToken = GenerateRefreshToken();

        await _refreshTokenRepository.SaveAsync(
            new RefreshTokenRecord(refreshToken, teacher.Year, teacher.EmployeeNo, now, refreshExpiresAtUtc, null),
            cancellationToken);

        await WriteAuditAsync(
            idNo: teacher.IdNo,
            verifyMethod: challenge.VerifiedBy,
            targetEmail: teacher.Email,
            success: true,
            failureReason: null,
            timestampUtc: now,
            eventType: "session.exchange",
            clientContext,
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

    public async Task<SessionRefreshResult> RefreshAsync(string refreshToken, AuthClientContext clientContext, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var activeToken = await _refreshTokenRepository.GetActiveAsync(refreshToken, now, cancellationToken);
        if (activeToken is null)
        {
            await WriteAuditAsync(
                idNo: null,
                verifyMethod: VerifyMethod.None,
                targetEmail: null,
                success: false,
                failureReason: "refresh_token_invalid",
                timestampUtc: now,
                eventType: "session.refresh",
                clientContext,
                cancellationToken);

            return new SessionRefreshResult(false, "Refresh token is invalid.", null, null, null, null, null);
        }

        var teacher = await _teacherRepository.GetByEmployeeNoAsync(activeToken.Year, activeToken.EmployeeNo, cancellationToken);
        if (teacher is null || !teacher.IsActive)
        {
            await WriteAuditAsync(
                idNo: null,
                verifyMethod: VerifyMethod.None,
                targetEmail: null,
                success: false,
                failureReason: "account_not_active",
                timestampUtc: now,
                eventType: "session.refresh",
                clientContext,
                cancellationToken);

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

        await WriteAuditAsync(
            idNo: teacher.IdNo,
            verifyMethod: VerifyMethod.None,
            targetEmail: teacher.Email,
            success: true,
            failureReason: null,
            timestampUtc: now,
            eventType: "session.refresh",
            clientContext,
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

    public async Task<SessionLogoutResult> LogoutAsync(string? refreshToken, AuthClientContext clientContext, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        string? idNo = null;
        string? email = null;

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var activeToken = await _refreshTokenRepository.GetActiveAsync(refreshToken, now, cancellationToken);
            if (activeToken is not null)
            {
                var teacher = await _teacherRepository.GetByEmployeeNoAsync(activeToken.Year, activeToken.EmployeeNo, cancellationToken);
                idNo = teacher?.IdNo;
                email = teacher?.Email;
                await _refreshTokenRepository.RevokeByUserAsync(activeToken.Year, activeToken.EmployeeNo, now, cancellationToken);
            }
            else
            {
                await _refreshTokenRepository.RevokeAsync(refreshToken, now, cancellationToken);
            }
        }

        await WriteAuditAsync(
            idNo,
            VerifyMethod.None,
            email,
            success: true,
            failureReason: null,
            timestampUtc: now,
            eventType: "session.logout",
            clientContext,
            cancellationToken);

        return new SessionLogoutResult(true, "Logged out.");
    }

    private async Task WriteAuditAsync(
        string? idNo,
        VerifyMethod verifyMethod,
        string? targetEmail,
        bool success,
        string? failureReason,
        DateTime timestampUtc,
        string eventType,
        AuthClientContext clientContext,
        CancellationToken cancellationToken)
    {
        await _auditLogRepository.WriteAsync(
            new AuditLogEntry(
                idNo,
                verifyMethod,
                targetEmail,
                clientContext.ClientIp,
                clientContext.UserAgent,
                success,
                failureReason,
                timestampUtc,
                eventType,
                new Dictionary<string, string?>
                {
                    ["eventType"] = eventType
                }),
            cancellationToken);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(bytes);
    }
}
