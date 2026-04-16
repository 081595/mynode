using TeacherAppointment.Application.Abstractions.Persistence;

namespace TeacherAppointment.Infrastructure.Persistence;

public sealed class SqliteRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteRefreshTokenRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(RefreshTokenRecord token, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
INSERT INTO refresh_tokens (refresh_token, yr, empl_no, issued_at_utc, expires_at_utc, revoked_at_utc)
VALUES ($refreshToken, $yr, $emplNo, $issuedAtUtc, $expiresAtUtc, $revokedAtUtc)
ON CONFLICT(refresh_token) DO UPDATE SET
    yr = excluded.yr,
    empl_no = excluded.empl_no,
    issued_at_utc = excluded.issued_at_utc,
    expires_at_utc = excluded.expires_at_utc,
    revoked_at_utc = excluded.revoked_at_utc;
""";

        command.Parameters.AddWithValue("$refreshToken", token.RefreshToken);
        command.Parameters.AddWithValue("$yr", token.Year);
        command.Parameters.AddWithValue("$emplNo", token.EmployeeNo);
        command.Parameters.AddWithValue("$issuedAtUtc", token.IssuedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$expiresAtUtc", token.ExpiresAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$revokedAtUtc", token.RevokedAtUtc?.ToString("O") ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<RefreshTokenRecord?> GetActiveAsync(string refreshToken, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT refresh_token, yr, empl_no, issued_at_utc, expires_at_utc, revoked_at_utc
FROM refresh_tokens
WHERE refresh_token = $refreshToken
  AND revoked_at_utc IS NULL
  AND expires_at_utc > $nowUtc
LIMIT 1;
""";
        command.Parameters.AddWithValue("$refreshToken", refreshToken);
        command.Parameters.AddWithValue("$nowUtc", nowUtc.ToString("O"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new RefreshTokenRecord(
            reader.GetString(0),
            reader.GetInt32(1),
            reader.GetString(2),
            DateTime.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind),
            DateTime.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind),
            reader.IsDBNull(5)
                ? null
                : DateTime.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind));
    }

    public async Task RevokeAsync(string refreshToken, DateTime revokedAtUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
UPDATE refresh_tokens
SET revoked_at_utc = $revokedAtUtc
WHERE refresh_token = $refreshToken;
""";
        command.Parameters.AddWithValue("$refreshToken", refreshToken);
        command.Parameters.AddWithValue("$revokedAtUtc", revokedAtUtc.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RevokeByUserAsync(int year, string employeeNo, DateTime revokedAtUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
UPDATE refresh_tokens
SET revoked_at_utc = $revokedAtUtc
WHERE yr = $yr
  AND empl_no = $emplNo
  AND revoked_at_utc IS NULL;
""";
        command.Parameters.AddWithValue("$yr", year);
        command.Parameters.AddWithValue("$emplNo", employeeNo);
        command.Parameters.AddWithValue("$revokedAtUtc", revokedAtUtc.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
