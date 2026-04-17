using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TeacherAppointment.Infrastructure.Persistence;

public sealed class SqliteDbInitializer : IHostedService
{
    private readonly ISqliteConnectionFactory _connectionFactory;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IOptions<RoleBasedTestAccountProvisioningOptions> _provisioningOptions;
    private readonly ILogger<SqliteDbInitializer> _logger;

    public SqliteDbInitializer(
        ISqliteConnectionFactory connectionFactory,
        IHostEnvironment hostEnvironment,
        IOptions<RoleBasedTestAccountProvisioningOptions> provisioningOptions,
        ILogger<SqliteDbInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _hostEnvironment = hostEnvironment;
        _provisioningOptions = provisioningOptions;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        await ProvisionRoleBasedTestAccountsAsync(connection, cancellationToken);
        await SeedAppointmentsAsync(connection, cancellationToken);
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

CREATE TABLE IF NOT EXISTS teach_appo_resp (
    yr INTEGER NOT NULL,
    empl_no TEXT NOT NULL,
    appo_doc_yy INTEGER NOT NULL,
    appo_doc_ch TEXT NOT NULL,
    appo_doc_seq TEXT NOT NULL,
    file_name TEXT NULL,
    pdf_content BLOB NULL,
    resp_status INTEGER NOT NULL DEFAULT 0,
    download_count INTEGER NOT NULL DEFAULT 0,
    remark TEXT NULL,
    create_date TEXT NOT NULL,
    update_date TEXT NOT NULL,
    PRIMARY KEY (yr, empl_no, appo_doc_yy, appo_doc_ch, appo_doc_seq)
);

CREATE INDEX IF NOT EXISTS idx_teach_appo_resp_teacher ON teach_appo_resp (yr, empl_no, resp_status);
""";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ProvisionRoleBasedTestAccountsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var options = _provisioningOptions.Value;

        if (!options.Enabled)
        {
            _logger.LogInformation(
                "Role-based test account provisioning skipped: enabled flag is false. env={Environment}",
                _hostEnvironment.EnvironmentName);
            return;
        }

        var eligible = options.EligibleEnvironments.Any(env =>
            string.Equals(env, _hostEnvironment.EnvironmentName, StringComparison.OrdinalIgnoreCase));

        if (!eligible)
        {
            _logger.LogWarning(
                "Role-based test account provisioning blocked by environment guardrail. env={Environment} eligible={EligibleEnvironments}",
                _hostEnvironment.EnvironmentName,
                string.Join(",", options.EligibleEnvironments));
            return;
        }

        _logger.LogInformation(
            "Role-based test account provisioning enabled. env={Environment} templatePrefix={TemplatePrefix}",
            _hostEnvironment.EnvironmentName,
            "TST");

        var seeds = new[]
        {
            new { Yr = 115, EmplNo = "TST-U-0001", IdNo = "A123456789", Birthday = "1985-03-17", Name = "Alex Teacher", Email = "alex.teacher@example.edu", Role = "user" },
            new { Yr = 115, EmplNo = "TST-A-0001", IdNo = "B223456789", Birthday = "1978-10-04", Name = "Admin User", Email = "admin.user@example.edu", Role = "admin" }
        };

        var outcomeByRole = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var seed in seeds)
        {
            try
            {
                var result = await UpsertTeacherSeedAsync(connection, seed, cancellationToken);
                outcomeByRole[seed.Role] = result;
                _logger.LogInformation(
                    "Role-based test account provisioning result: action={Action} role={Role} year={Year} employeeNo={EmployeeNo}",
                    result,
                    seed.Role,
                    seed.Yr,
                    seed.EmplNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Role-based test account provisioning failed: role={Role} year={Year} employeeNo={EmployeeNo}",
                    seed.Role,
                    seed.Yr,
                    seed.EmplNo);
                throw;
            }
        }

        _logger.LogInformation(
            "Role-based test account provisioning summary: user={UserOutcome} admin={AdminOutcome}",
            outcomeByRole.GetValueOrDefault("user", "skipped"),
            outcomeByRole.GetValueOrDefault("admin", "skipped"));
    }

    private static async Task<string> UpsertTeacherSeedAsync(
        SqliteConnection connection,
        dynamic seed,
        CancellationToken cancellationToken)
    {
        await using var readCmd = connection.CreateCommand();
        readCmd.CommandText = """
SELECT id_no, birthday, ch_name, email, role, is_active
FROM teach_appo_empl_base
WHERE yr = $yr AND empl_no = $emplNo
LIMIT 1;
""";
        readCmd.Parameters.AddWithValue("$yr", (int)seed.Yr);
        readCmd.Parameters.AddWithValue("$emplNo", (string)seed.EmplNo);

        await using var reader = await readCmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            await using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = """
INSERT INTO teach_appo_empl_base (yr, empl_no, id_no, birthday, ch_name, email, role, is_active)
VALUES ($yr, $emplNo, $idNo, $birthday, $name, $email, $role, 1);
""";
            insertCmd.Parameters.AddWithValue("$yr", (int)seed.Yr);
            insertCmd.Parameters.AddWithValue("$emplNo", (string)seed.EmplNo);
            insertCmd.Parameters.AddWithValue("$idNo", (string)seed.IdNo);
            insertCmd.Parameters.AddWithValue("$birthday", (string)seed.Birthday);
            insertCmd.Parameters.AddWithValue("$name", (string)seed.Name);
            insertCmd.Parameters.AddWithValue("$email", (string)seed.Email);
            insertCmd.Parameters.AddWithValue("$role", (string)seed.Role);
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            return "created";
        }

        var idNo = reader.GetString(0);
        var birthday = reader.GetString(1);
        var name = reader.GetString(2);
        var email = reader.IsDBNull(3) ? null : reader.GetString(3);
        var role = reader.GetString(4);
        var isActive = reader.GetInt32(5) == 1;
        await reader.CloseAsync();

        var isUnchanged = string.Equals(idNo, (string)seed.IdNo, StringComparison.Ordinal)
            && string.Equals(birthday, (string)seed.Birthday, StringComparison.Ordinal)
            && string.Equals(name, (string)seed.Name, StringComparison.Ordinal)
            && string.Equals(email, (string)seed.Email, StringComparison.Ordinal)
            && string.Equals(role, (string)seed.Role, StringComparison.Ordinal)
            && isActive;

        if (isUnchanged)
        {
            return "skipped";
        }

        await using var updateCmd = connection.CreateCommand();
        updateCmd.CommandText = """
UPDATE teach_appo_empl_base
SET id_no = $idNo,
    birthday = $birthday,
    ch_name = $name,
    email = $email,
    role = $role,
    is_active = 1
WHERE yr = $yr AND empl_no = $emplNo;
""";
        updateCmd.Parameters.AddWithValue("$yr", (int)seed.Yr);
        updateCmd.Parameters.AddWithValue("$emplNo", (string)seed.EmplNo);
        updateCmd.Parameters.AddWithValue("$idNo", (string)seed.IdNo);
        updateCmd.Parameters.AddWithValue("$birthday", (string)seed.Birthday);
        updateCmd.Parameters.AddWithValue("$name", (string)seed.Name);
        updateCmd.Parameters.AddWithValue("$email", (string)seed.Email);
        updateCmd.Parameters.AddWithValue("$role", (string)seed.Role);
        await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        return "updated";
    }

    private static async Task SeedAppointmentsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow.ToString("O");
        var seeds = new[]
        {
            new
            {
                Yr = 115,
                EmplNo = "TST-U-0001",
                DocYear = 115,
                DocType = "教字",
                DocSeq = "0001",
                FileName = "appointment-115-TST-U-0001-0001.pdf",
                Pdf = "Sample appointment PDF #1",
                RespStatus = 0,
                DownloadCount = 0,
                Remark = "Pending teacher response"
            },
            new
            {
                Yr = 115,
                EmplNo = "TST-U-0001",
                DocYear = 115,
                DocType = "教字",
                DocSeq = "0002",
                FileName = "appointment-115-TST-U-0001-0002.pdf",
                Pdf = "Sample appointment PDF #2",
                RespStatus = 1,
                DownloadCount = 2,
                Remark = "Completed"
            }
        };

        foreach (var seed in seeds)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = """
INSERT INTO teach_appo_resp (
    yr, empl_no, appo_doc_yy, appo_doc_ch, appo_doc_seq,
    file_name, pdf_content, resp_status, download_count, remark, create_date, update_date)
VALUES (
    $yr, $emplNo, $docYear, $docType, $docSeq,
    $fileName, $pdfContent, $respStatus, $downloadCount, $remark, $createDate, $updateDate)
ON CONFLICT(yr, empl_no, appo_doc_yy, appo_doc_ch, appo_doc_seq) DO UPDATE SET
    file_name = excluded.file_name,
    pdf_content = excluded.pdf_content,
    resp_status = excluded.resp_status,
    download_count = excluded.download_count,
    remark = excluded.remark,
    update_date = excluded.update_date;
""";
            cmd.Parameters.AddWithValue("$yr", seed.Yr);
            cmd.Parameters.AddWithValue("$emplNo", seed.EmplNo);
            cmd.Parameters.AddWithValue("$docYear", seed.DocYear);
            cmd.Parameters.AddWithValue("$docType", seed.DocType);
            cmd.Parameters.AddWithValue("$docSeq", seed.DocSeq);
            cmd.Parameters.AddWithValue("$fileName", seed.FileName);
            cmd.Parameters.AddWithValue("$pdfContent", System.Text.Encoding.UTF8.GetBytes(seed.Pdf));
            cmd.Parameters.AddWithValue("$respStatus", seed.RespStatus);
            cmd.Parameters.AddWithValue("$downloadCount", seed.DownloadCount);
            cmd.Parameters.AddWithValue("$remark", seed.Remark);
            cmd.Parameters.AddWithValue("$createDate", now);
            cmd.Parameters.AddWithValue("$updateDate", now);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
