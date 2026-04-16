using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;

namespace TeacherAppointment.Infrastructure.Persistence;

public sealed class SqliteDbInitializer : IHostedService
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteDbInitializer(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        await SeedTeachersAsync(connection, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureSchemaAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
CREATE TABLE IF NOT EXISTS teach_appo_empl_base (
    yr INTEGER NOT NULL,
    empl_no TEXT NOT NULL,
    id_no TEXT NOT NULL,
    birthday TEXT NOT NULL,
    ch_name TEXT NOT NULL,
    email TEXT NULL,
    role TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    PRIMARY KEY (yr, empl_no)
);

CREATE INDEX IF NOT EXISTS idx_empl_identity ON teach_appo_empl_base (id_no, birthday, is_active);

CREATE TABLE IF NOT EXISTS auth_challenges (
    challenge_id TEXT PRIMARY KEY,
    yr INTEGER NOT NULL,
    empl_no TEXT NOT NULL,
    id_no TEXT NOT NULL,
    target_email TEXT NULL,
    verification_code TEXT NOT NULL,
    expires_at_utc TEXT NOT NULL,
    resend_available_at_utc TEXT NOT NULL,
    is_verified INTEGER NOT NULL,
    verified_by INTEGER NOT NULL,
    verified_at_utc TEXT NULL,
    qr_session_id TEXT NULL,
    qr_session_expires_at_utc TEXT NULL,
    is_completed INTEGER NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_auth_challenges_qr_session ON auth_challenges (qr_session_id);

CREATE TABLE IF NOT EXISTS refresh_tokens (
    refresh_token TEXT PRIMARY KEY,
    yr INTEGER NOT NULL,
    empl_no TEXT NOT NULL,
    issued_at_utc TEXT NOT NULL,
    expires_at_utc TEXT NOT NULL,
    revoked_at_utc TEXT NULL
);

CREATE INDEX IF NOT EXISTS idx_refresh_token_lookup ON refresh_tokens (refresh_token, expires_at_utc, revoked_at_utc);
CREATE INDEX IF NOT EXISTS idx_refresh_token_user ON refresh_tokens (yr, empl_no, revoked_at_utc);

CREATE TABLE IF NOT EXISTS login_logs (
    log_id INTEGER PRIMARY KEY AUTOINCREMENT,
    id_no TEXT NULL,
    verify_method INTEGER NOT NULL,
    target_email TEXT NULL,
    client_ip TEXT NOT NULL,
    user_agent TEXT NOT NULL,
    success INTEGER NOT NULL,
    failure_reason TEXT NULL,
    timestamp_utc TEXT NOT NULL,
    event_type TEXT NOT NULL,
    metadata_json TEXT NULL
);
""";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SeedTeachersAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var seeds = new[]
        {
            new { Yr = 115, EmplNo = "E12345", IdNo = "A123456789", Birthday = "1985-03-17", Name = "Alex Teacher", Email = "alex.teacher@example.edu", Role = "user" },
            new { Yr = 115, EmplNo = "A00001", IdNo = "B223456789", Birthday = "1978-10-04", Name = "Admin User", Email = "admin.user@example.edu", Role = "admin" }
        };

        foreach (var seed in seeds)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = """
INSERT INTO teach_appo_empl_base (yr, empl_no, id_no, birthday, ch_name, email, role, is_active)
VALUES ($yr, $emplNo, $idNo, $birthday, $name, $email, $role, 1)
ON CONFLICT(yr, empl_no) DO UPDATE SET
    id_no = excluded.id_no,
    birthday = excluded.birthday,
    ch_name = excluded.ch_name,
    email = excluded.email,
    role = excluded.role,
    is_active = excluded.is_active;
""";
            cmd.Parameters.AddWithValue("$yr", seed.Yr);
            cmd.Parameters.AddWithValue("$emplNo", seed.EmplNo);
            cmd.Parameters.AddWithValue("$idNo", seed.IdNo);
            cmd.Parameters.AddWithValue("$birthday", seed.Birthday);
            cmd.Parameters.AddWithValue("$name", seed.Name);
            cmd.Parameters.AddWithValue("$email", seed.Email);
            cmd.Parameters.AddWithValue("$role", seed.Role);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
