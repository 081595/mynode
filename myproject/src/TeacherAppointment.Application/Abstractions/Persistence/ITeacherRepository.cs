namespace TeacherAppointment.Application.Abstractions.Persistence;

public interface ITeacherRepository
{
    Task<TeacherIdentityRecord?> FindByIdentityAsync(string idNo, DateOnly birthday, CancellationToken cancellationToken = default);

    Task<TeacherIdentityRecord?> GetByEmployeeNoAsync(int year, string employeeNo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeacherIdentityRecord>> GetForAdminAsync(int year, string? employeeNo, CancellationToken cancellationToken = default);

    Task<TeacherIdentityRecord> UpsertForAdminAsync(TeacherAdminUpsertInput input, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int year, string employeeNo, CancellationToken cancellationToken = default);
}

public sealed record TeacherIdentityRecord(
    int Year,
    string EmployeeNo,
    string IdNo,
    string IdNoMasked,
    DateOnly Birthday,
    string Name,
    string? Email,
    string Role,
    bool IsActive);

public sealed record TeacherAdminUpsertInput(
    int Year,
    string EmployeeNo,
    string IdNo,
    DateOnly Birthday,
    string Name,
    string? Email,
    string Role,
    bool IsActive);
