namespace TeacherAppointment.Application.Abstractions.Persistence;

public interface ITeacherRepository
{
    Task<TeacherIdentityRecord?> FindByIdentityAsync(string idNo, DateOnly birthday, CancellationToken cancellationToken = default);

    Task<TeacherIdentityRecord?> GetByEmployeeNoAsync(int year, string employeeNo, CancellationToken cancellationToken = default);
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
