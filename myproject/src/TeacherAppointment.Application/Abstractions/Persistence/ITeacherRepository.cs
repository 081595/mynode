namespace TeacherAppointment.Application.Abstractions.Persistence;

public interface ITeacherRepository
{
    Task<bool> ExistsByIdentityAsync(string idNo, DateOnly birthday, CancellationToken cancellationToken = default);

    Task<TeacherRecord?> GetByEmployeeNoAsync(int year, string employeeNo, CancellationToken cancellationToken = default);
}

public sealed record TeacherRecord(
    int Year,
    string EmployeeNo,
    string IdNoMasked,
    string Name,
    string? Email,
    string Role);
