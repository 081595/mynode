namespace TeacherAppointment.Application.Abstractions.Persistence;

public interface IAuditLogRepository
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}

public sealed record AuditLogEntry(
    string? IdNo,
    VerifyMethod VerifyMethod,
    string? TargetEmail,
    string ClientIp,
    string UserAgent,
    bool Success,
    string? FailureReason,
    DateTime TimestampUtc,
    string EventType,
    IReadOnlyDictionary<string, string?> Metadata);
