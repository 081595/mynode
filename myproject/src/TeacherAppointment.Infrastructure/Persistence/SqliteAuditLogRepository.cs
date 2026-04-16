using System.Text.Json;
using TeacherAppointment.Application.Abstractions.Persistence;

namespace TeacherAppointment.Infrastructure.Persistence;

public sealed class SqliteAuditLogRepository : IAuditLogRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteAuditLogRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
INSERT INTO login_logs (
    id_no, verify_method, target_email, client_ip, user_agent,
    success, failure_reason, timestamp_utc, event_type, metadata_json)
VALUES (
    $idNo, $verifyMethod, $targetEmail, $clientIp, $userAgent,
    $success, $failureReason, $timestampUtc, $eventType, $metadataJson);
""";

        command.Parameters.AddWithValue("$idNo", (object?)entry.IdNo ?? DBNull.Value);
        command.Parameters.AddWithValue("$verifyMethod", (int)entry.VerifyMethod);
        command.Parameters.AddWithValue("$targetEmail", (object?)entry.TargetEmail ?? DBNull.Value);
        command.Parameters.AddWithValue("$clientIp", entry.ClientIp);
        command.Parameters.AddWithValue("$userAgent", entry.UserAgent);
        command.Parameters.AddWithValue("$success", entry.Success ? 1 : 0);
        command.Parameters.AddWithValue("$failureReason", (object?)entry.FailureReason ?? DBNull.Value);
        command.Parameters.AddWithValue("$timestampUtc", entry.TimestampUtc.ToString("O"));
        command.Parameters.AddWithValue("$eventType", entry.EventType);
        command.Parameters.AddWithValue("$metadataJson", JsonSerializer.Serialize(entry.Metadata));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
