using Microsoft.Data.Sqlite;
using TeacherAppointment.Application.Abstractions.Persistence;

namespace TeacherAppointment.Infrastructure.Persistence;

public sealed class SqliteAuthChallengeRepository : IAuthChallengeRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteAuthChallengeRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AuthChallengeRecord> SaveAsync(AuthChallengeRecord challenge, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
INSERT INTO auth_challenges (
    challenge_id, yr, empl_no, id_no, target_email, verification_code, expires_at_utc,
    resend_available_at_utc, is_verified, verified_by, verified_at_utc,
    qr_session_id, qr_session_expires_at_utc, is_completed)
VALUES (
    $challengeId, $yr, $emplNo, $idNo, $targetEmail, $verificationCode, $expiresAtUtc,
    $resendAvailableAtUtc, $isVerified, $verifiedBy, $verifiedAtUtc,
    $qrSessionId, $qrSessionExpiresAtUtc, $isCompleted)
ON CONFLICT(challenge_id) DO UPDATE SET
    yr = excluded.yr,
    empl_no = excluded.empl_no,
    id_no = excluded.id_no,
    target_email = excluded.target_email,
    verification_code = excluded.verification_code,
    expires_at_utc = excluded.expires_at_utc,
    resend_available_at_utc = excluded.resend_available_at_utc,
    is_verified = excluded.is_verified,
    verified_by = excluded.verified_by,
    verified_at_utc = excluded.verified_at_utc,
    qr_session_id = excluded.qr_session_id,
    qr_session_expires_at_utc = excluded.qr_session_expires_at_utc,
    is_completed = excluded.is_completed;
""";

        command.Parameters.AddWithValue("$challengeId", challenge.ChallengeId);
        command.Parameters.AddWithValue("$yr", challenge.Year);
        command.Parameters.AddWithValue("$emplNo", challenge.EmployeeNo);
        command.Parameters.AddWithValue("$idNo", challenge.IdNo);
        command.Parameters.AddWithValue("$targetEmail", (object?)challenge.TargetEmail ?? DBNull.Value);
        command.Parameters.AddWithValue("$verificationCode", challenge.VerificationCode);
        command.Parameters.AddWithValue("$expiresAtUtc", challenge.ExpiresAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$resendAvailableAtUtc", challenge.ResendAvailableAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$isVerified", challenge.IsVerified ? 1 : 0);
        command.Parameters.AddWithValue("$verifiedBy", (int)challenge.VerifiedBy);
        command.Parameters.AddWithValue("$verifiedAtUtc", challenge.VerifiedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$qrSessionId", (object?)challenge.QrSessionId ?? DBNull.Value);
        command.Parameters.AddWithValue("$qrSessionExpiresAtUtc", challenge.QrSessionExpiresAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$isCompleted", challenge.IsCompleted ? 1 : 0);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return challenge;
    }

    public async Task<AuthChallengeRecord?> GetByIdAsync(string challengeId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT challenge_id, yr, empl_no, id_no, target_email, verification_code, expires_at_utc,
       resend_available_at_utc, is_verified, verified_by, verified_at_utc,
       qr_session_id, qr_session_expires_at_utc, is_completed
FROM auth_challenges
WHERE challenge_id = $challengeId
LIMIT 1;
""";
        command.Parameters.AddWithValue("$challengeId", challengeId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task<AuthChallengeRecord?> GetByQrSessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT challenge_id, yr, empl_no, id_no, target_email, verification_code, expires_at_utc,
       resend_available_at_utc, is_verified, verified_by, verified_at_utc,
       qr_session_id, qr_session_expires_at_utc, is_completed
FROM auth_challenges
WHERE qr_session_id = $sessionId
LIMIT 1;
""";
        command.Parameters.AddWithValue("$sessionId", sessionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    private static AuthChallengeRecord Map(SqliteDataReader reader)
    {
        return new AuthChallengeRecord(
            reader.GetString(0),
            reader.GetInt32(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.GetString(5),
            DateTime.Parse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind),
            DateTime.Parse(reader.GetString(7), null, System.Globalization.DateTimeStyles.RoundtripKind),
            reader.GetInt32(8) == 1,
            (VerifyMethod)reader.GetInt32(9),
            reader.IsDBNull(10)
                ? null
                : DateTime.Parse(reader.GetString(10), null, System.Globalization.DateTimeStyles.RoundtripKind),
            reader.IsDBNull(11) ? null : reader.GetString(11),
            reader.IsDBNull(12)
                ? null
                : DateTime.Parse(reader.GetString(12), null, System.Globalization.DateTimeStyles.RoundtripKind),
            reader.GetInt32(13) == 1);
    }
}
