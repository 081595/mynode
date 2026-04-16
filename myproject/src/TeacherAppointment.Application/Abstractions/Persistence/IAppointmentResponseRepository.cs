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
}

public sealed record AppointmentResponseSummary(
    int Year,
    string EmployeeNo,
    string DocumentNo,
    bool Responded,
    int DownloadCount,
    DateTime UpdatedAtUtc);
