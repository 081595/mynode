using Microsoft.Data.Sqlite;

namespace TeacherAppointment.Infrastructure.Persistence;

public interface ISqliteConnectionFactory
{
    Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
