using System.Security.Cryptography;
using TeacherAppointment.Application.Abstractions.Infrastructure;
using TeacherAppointment.Application.Abstractions.Persistence;
using TeacherAppointment.Application.Abstractions.Realtime;

namespace TeacherAppointment.Application.Features.Auth;

public sealed class IdentityChallengeService : IIdentityChallengeService
{
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(3);
    private const string GenericIdentifyFailureMessage = "Unable to start verification challenge.";
    private const string RedirectUrl = "/auth/complete";

    private readonly ITeacherRepository _teacherRepository;
    private readonly IAuthChallengeRepository _authChallengeRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IEmailSender _emailSender;
    private readonly IAuthChallengeNotifier _notifier;
    private readonly TimeProvider _timeProvider;

    public IdentityChallengeService(
        ITeacherRepository teacherRepository,
        IAuthChallengeRepository authChallengeRepository,
        IAuditLogRepository auditLogRepository,
        IEmailSender emailSender,
        IAuthChallengeNotifier notifier,
        TimeProvider timeProvider)
    {
        _teacherRepository = teacherRepository;
        _authChallengeRepository = authChallengeRepository;
        _auditLogRepository = auditLogRepository;
        _emailSender = emailSender;
        _notifier = notifier;
        _timeProvider = timeProvider;
    }

    public async Task<ChallengeInitResult> InitializeAsync(string idNo, DateOnly birthday, CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.FindByIdentityAsync(idNo, birthday, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (teacher is null || !teacher.IsActive)
        {
            await WriteAuditAsync(idNo, VerifyMethod.None, null, false, "identity_not_matched", now, "challenge.initialize", cancellationToken);
            return new ChallengeInitResult(false, GenericIdentifyFailureMessage, null, null, null, null, null);
        }

        var challenge = new AuthChallengeRecord(
            ChallengeId: Guid.NewGuid().ToString("N"),
            Year: teacher.Year,
            EmployeeNo: teacher.EmployeeNo,
            IdNo: teacher.IdNo,
            TargetEmail: teacher.Email,
            VerificationCode: RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6"),
            ExpiresAtUtc: now.Add(ChallengeLifetime),
            ResendAvailableAtUtc: now.Add(ResendCooldown),
            IsVerified: false,
            VerifiedBy: VerifyMethod.None,
            VerifiedAtUtc: null,
            QrSessionId: null,
            QrSessionExpiresAtUtc: null,
            IsCompleted: false);

        await _authChallengeRepository.SaveAsync(challenge, cancellationToken);

        if (!string.IsNullOrWhiteSpace(teacher.Email))
        {
            await _emailSender.SendVerificationCodeAsync(teacher.Email, challenge.VerificationCode, cancellationToken);
        }

        await WriteAuditAsync(teacher.IdNo, VerifyMethod.None, teacher.Email, true, null, now, "challenge.initialize", cancellationToken);

        return new ChallengeInitResult(
            true,
            "Verification challenge created.",
            challenge.ChallengeId,
            teacher.EmployeeNo,
            MaskEmail(teacher.Email),
            challenge.ExpiresAtUtc,
            challenge.ResendAvailableAtUtc);
    }

    public async Task<EmailVerificationResult> VerifyEmailCodeAsync(string challengeId, string code, CancellationToken cancellationToken = default)
    {
        var challenge = await _authChallengeRepository.GetByIdAsync(challengeId, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (challenge is null)
        {
            return new EmailVerificationResult(false, "Challenge not found.", challengeId, false, null);
        }

        if (challenge.IsVerified || challenge.IsCompleted)
        {
            await WriteAuditAsync(challenge.IdNo, VerifyMethod.Email, challenge.TargetEmail, false, "challenge_already_completed", now, "challenge.verify.email", cancellationToken);
            return new EmailVerificationResult(false, "Challenge already completed.", challengeId, true, challenge.VerifiedAtUtc);
        }

        if (challenge.ExpiresAtUtc <= now)
        {
            await WriteAuditAsync(challenge.IdNo, VerifyMethod.Email, challenge.TargetEmail, false, "challenge_expired", now, "challenge.verify.email", cancellationToken);
            return new EmailVerificationResult(false, "Challenge expired.", challengeId, false, null);
        }

        if (!string.Equals(challenge.VerificationCode, code, StringComparison.Ordinal))
        {
            await WriteAuditAsync(challenge.IdNo, VerifyMethod.Email, challenge.TargetEmail, false, "invalid_code", now, "challenge.verify.email", cancellationToken);
            return new EmailVerificationResult(false, "Invalid verification code.", challengeId, false, null);
        }

        var updated = challenge with
        {
            IsVerified = true,
            IsCompleted = true,
            VerifiedBy = VerifyMethod.Email,
            VerifiedAtUtc = now
        };

        await _authChallengeRepository.SaveAsync(updated, cancellationToken);
        await WriteAuditAsync(updated.IdNo, VerifyMethod.Email, updated.TargetEmail, true, null, now, "challenge.verify.email", cancellationToken);

        return new EmailVerificationResult(true, "Challenge verified.", challengeId, true, updated.VerifiedAtUtc);
    }

    public async Task<QrChallengeSessionResult> CreateQrSessionAsync(string challengeId, CancellationToken cancellationToken = default)
    {
        var challenge = await _authChallengeRepository.GetByIdAsync(challengeId, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (challenge is null)
        {
            return new QrChallengeSessionResult(false, "Challenge not found.", null, null, null);
        }

        if (challenge.IsVerified || challenge.IsCompleted)
        {
            return new QrChallengeSessionResult(false, "Challenge already completed.", challenge.ChallengeId, null, null);
        }

        if (challenge.ExpiresAtUtc <= now)
        {
            await WriteAuditAsync(challenge.IdNo, VerifyMethod.QrCode, challenge.TargetEmail, false, "challenge_expired", now, "challenge.qr.create", cancellationToken);
            return new QrChallengeSessionResult(false, "Challenge expired.", challenge.ChallengeId, null, null);
        }

        var qrExpiresAtUtc = challenge.ExpiresAtUtc;
        var updated = challenge with
        {
            QrSessionId = Guid.NewGuid().ToString("N"),
            QrSessionExpiresAtUtc = qrExpiresAtUtc
        };

        await _authChallengeRepository.SaveAsync(updated, cancellationToken);
        await WriteAuditAsync(updated.IdNo, VerifyMethod.QrCode, updated.TargetEmail, true, null, now, "challenge.qr.create", cancellationToken);

        return new QrChallengeSessionResult(true, "QR session created.", updated.ChallengeId, updated.QrSessionId, updated.QrSessionExpiresAtUtc);
    }

    public async Task<QrChallengeConfirmationResult> ConfirmQrSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var challenge = await _authChallengeRepository.GetByQrSessionIdAsync(sessionId, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (challenge is null)
        {
            return new QrChallengeConfirmationResult(false, "QR session not found.", null, sessionId, false, null, null);
        }

        if (challenge.IsVerified || challenge.IsCompleted)
        {
            return new QrChallengeConfirmationResult(false, "Challenge already completed.", challenge.ChallengeId, sessionId, true, challenge.VerifiedAtUtc, RedirectUrl);
        }

        if (challenge.QrSessionExpiresAtUtc is null || challenge.QrSessionExpiresAtUtc <= now || challenge.ExpiresAtUtc <= now)
        {
            await WriteAuditAsync(challenge.IdNo, VerifyMethod.QrCode, challenge.TargetEmail, false, "qr_session_expired", now, "challenge.qr.confirm", cancellationToken);
            return new QrChallengeConfirmationResult(false, "QR session expired.", challenge.ChallengeId, sessionId, false, null, null);
        }

        var updated = challenge with
        {
            IsVerified = true,
            IsCompleted = true,
            VerifiedBy = VerifyMethod.QrCode,
            VerifiedAtUtc = now
        };

        await _authChallengeRepository.SaveAsync(updated, cancellationToken);
        await _notifier.NotifyDesktopRedirectAsync(sessionId, RedirectUrl, cancellationToken);
        await _notifier.NotifyMobileCloseAsync(sessionId, cancellationToken);
        await WriteAuditAsync(updated.IdNo, VerifyMethod.QrCode, updated.TargetEmail, true, null, now, "challenge.qr.confirm", cancellationToken);

        return new QrChallengeConfirmationResult(true, "Challenge verified.", updated.ChallengeId, sessionId, true, updated.VerifiedAtUtc, RedirectUrl);
    }

    private async Task WriteAuditAsync(
        string? idNo,
        VerifyMethod verifyMethod,
        string? targetEmail,
        bool success,
        string? failureReason,
        DateTime timestampUtc,
        string eventType,
        CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, string?>
        {
            ["eventType"] = eventType
        };

        await _auditLogRepository.WriteAsync(
            new AuditLogEntry(
                idNo,
                verifyMethod,
                targetEmail,
                "unknown",
                "unknown",
                success,
                failureReason,
                timestampUtc,
                eventType,
                metadata),
            cancellationToken);
    }

    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var parts = email.Split('@', 2);
        if (parts.Length != 2)
        {
            return "***";
        }

        var user = parts[0];
        var maskedUser = user.Length switch
        {
            <= 1 => "*",
            2 => $"{user[0]}*",
            _ => $"{user[0]}***{user[^1]}"
        };

        return $"{maskedUser}@{parts[1]}";
    }
}
