using Microsoft.Data.Sqlite;
using TeacherAppointment.Application.Abstractions.Persistence;

namespace TeacherAppointment.Infrastructure.Persistence;

public sealed class SqliteTeacherRepository : ITeacherRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteTeacherRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TeacherIdentityRecord?> FindByIdentityAsync(string idNo, DateOnly birthday, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT yr, empl_no, id_no, birthday, ch_name, email, role, is_active
FROM teach_appo_empl_base
WHERE id_no = $idNo AND birthday = $birthday AND is_active = 1
LIMIT 1;
""";
        command.Parameters.AddWithValue("$idNo", idNo);
        command.Parameters.AddWithValue("$birthday", birthday.ToString("yyyy-MM-dd"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task<TeacherIdentityRecord?> GetByEmployeeNoAsync(int year, string employeeNo, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT yr, empl_no, id_no, birthday, ch_name, email, role, is_active
FROM teach_appo_empl_base
WHERE yr = $yr AND empl_no = $emplNo
LIMIT 1;
""";
        command.Parameters.AddWithValue("$yr", year);
        command.Parameters.AddWithValue("$emplNo", employeeNo);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    private static TeacherIdentityRecord Map(SqliteDataReader reader)
    {
        var year = reader.GetInt32(0);
        var employeeNo = reader.GetString(1);
        var idNo = reader.GetString(2);
        var birthday = DateOnly.Parse(reader.GetString(3));
        var name = reader.GetString(4);
        var email = reader.IsDBNull(5) ? null : reader.GetString(5);
        var role = reader.GetString(6);
        var isActive = reader.GetInt32(7) == 1;

        return new TeacherIdentityRecord(
            year,
            employeeNo,
            idNo,
            MaskIdNo(idNo),
            birthday,
            name,
            email,
            role,
            isActive);
    }

    private static string MaskIdNo(string idNo)
    {
        if (idNo.Length < 4)
        {
            return "****";
        }

        return $"{idNo[0]}{idNo[1]}*****{idNo[^2]}{idNo[^1]}";
    }
}
