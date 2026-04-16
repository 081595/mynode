using System.Collections.Concurrent;
using TeacherAppointment.Application.Abstractions.Persistence;

namespace TeacherAppointment.Infrastructure.Persistence;

public sealed class InMemoryTeacherRepository : ITeacherRepository
{
    private readonly ConcurrentDictionary<string, TeacherIdentityRecord> _teachers;

    public InMemoryTeacherRepository()
    {
        _teachers = new ConcurrentDictionary<string, TeacherIdentityRecord>(StringComparer.OrdinalIgnoreCase);

        Seed(new TeacherIdentityRecord(115, "E12345", "A123456789", "A1*****789", new DateOnly(1985, 3, 17), "Alex Teacher", "alex.teacher@example.edu", "user", true));
        Seed(new TeacherIdentityRecord(115, "A00001", "B223456789", "B2*****789", new DateOnly(1978, 10, 4), "Admin User", "admin.user@example.edu", "admin", true));
    }

    public Task<TeacherIdentityRecord?> FindByIdentityAsync(string idNo, DateOnly birthday, CancellationToken cancellationToken = default)
    {
        var teacher = _teachers.Values.FirstOrDefault(item =>
            string.Equals(item.IdNo, idNo, StringComparison.OrdinalIgnoreCase) && item.Birthday == birthday);

        return Task.FromResult<TeacherIdentityRecord?>(teacher);
    }

    public Task<TeacherIdentityRecord?> GetByEmployeeNoAsync(int year, string employeeNo, CancellationToken cancellationToken = default)
    {
        _teachers.TryGetValue(BuildEmployeeKey(year, employeeNo), out var teacher);
        return Task.FromResult<TeacherIdentityRecord?>(teacher);
    }

    private void Seed(TeacherIdentityRecord teacher)
    {
        _teachers[BuildEmployeeKey(teacher.Year, teacher.EmployeeNo)] = teacher;
    }

    private static string BuildEmployeeKey(int year, string employeeNo) => $"{year}:{employeeNo}";
}
