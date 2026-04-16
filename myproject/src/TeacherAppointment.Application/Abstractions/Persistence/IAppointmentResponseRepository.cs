namespace TeacherAppointment.Application.Abstractions.Persistence;

public interface IAppointmentResponseRepository
{
    Task<IReadOnlyList<AppointmentResponseSummary>> GetForTeacherAsync(
        int year,
        string employeeNo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppointmentResponseSummary>> GetForAdminAsync(
        int year,
        string? employeeNo,
        CancellationToken cancellationToken = default);

    Task<AppointmentPdfPayload?> GetPdfAsync(
        AppointmentDocumentKey key,
        bool incrementDownloadCount,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<AppointmentCompletionResult> MarkResponseCompletedAsync(
        AppointmentDocumentKey key,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
}

public sealed record AppointmentResponseSummary(
    int Year,
    string EmployeeNo,
    int DocumentYear,
    string DocumentType,
    string DocumentSequence,
    string DocumentNo,
    bool Responded,
    int DownloadCount,
    string? Remark,
    DateTime UpdatedAtUtc);

public sealed record AppointmentDocumentKey(
    int Year,
    string EmployeeNo,
    int DocumentYear,
    string DocumentType,
    string DocumentSequence);

public sealed record AppointmentPdfPayload(
    AppointmentDocumentKey Key,
    string FileName,
    byte[] PdfContent,
    int DownloadCount,
    bool Responded,
    DateTime UpdatedAtUtc);

public sealed record AppointmentCompletionResult(
    bool Found,
    bool IsCompleted,
    bool Changed,
    DateTime? UpdatedAtUtc);
