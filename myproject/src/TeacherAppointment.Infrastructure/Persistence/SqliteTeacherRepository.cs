using Microsoft.Data.Sqlite;
using TeacherAppointment.Application.Abstractions.Persistence;
using TeacherAppointment.Application.Abstractions.Security;

namespace TeacherAppointment.Infrastructure.Persistence;

public sealed class SqliteTeacherRepository : ITeacherRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;
    private readonly ISensitiveDataMaskingPolicy _maskingPolicy;

    public SqliteTeacherRepository(ISqliteConnectionFactory connectionFactory, ISensitiveDataMaskingPolicy maskingPolicy)
    {
        _connectionFactory = connectionFactory;
        _maskingPolicy = maskingPolicy;
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

    public async Task<IReadOnlyList<TeacherIdentityRecord>> GetForAdminAsync(int year, string? employeeNo, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        if (string.IsNullOrWhiteSpace(employeeNo))
        {
            command.CommandText = """
SELECT yr, empl_no, id_no, birthday, ch_name, email, role, is_active
FROM teach_appo_empl_base
WHERE yr = $yr
ORDER BY empl_no;
""";
            command.Parameters.AddWithValue("$yr", year);
        }
        else
        {
            command.CommandText = """
SELECT yr, empl_no, id_no, birthday, ch_name, email, role, is_active
FROM teach_appo_empl_base
WHERE yr = $yr AND empl_no = $emplNo
ORDER BY empl_no;
""";
            command.Parameters.AddWithValue("$yr", year);
            command.Parameters.AddWithValue("$emplNo", employeeNo);
        }

        var rows = new List<TeacherIdentityRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(Map(reader));
        }

        return rows;
    }

    public async Task<TeacherIdentityRecord> UpsertForAdminAsync(TeacherAdminUpsertInput input, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
INSERT INTO teach_appo_empl_base (yr, empl_no, id_no, birthday, ch_name, email, role, is_active)
VALUES ($yr, $emplNo, $idNo, $birthday, $name, $email, $role, $isActive)
ON CONFLICT(yr, empl_no) DO UPDATE SET
    id_no = excluded.id_no,
    birthday = excluded.birthday,
    ch_name = excluded.ch_name,
    email = excluded.email,
    role = excluded.role,
    is_active = excluded.is_active;
""";

        command.Parameters.AddWithValue("$yr", input.Year);
        command.Parameters.AddWithValue("$emplNo", input.EmployeeNo);
        command.Parameters.AddWithValue("$idNo", input.IdNo);
        command.Parameters.AddWithValue("$birthday", input.Birthday.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$name", input.Name);
        command.Parameters.AddWithValue("$email", (object?)input.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("$role", input.Role);
        command.Parameters.AddWithValue("$isActive", input.IsActive ? 1 : 0);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return new TeacherIdentityRecord(
            input.Year,
            input.EmployeeNo,
            input.IdNo,
            _maskingPolicy.MaskIdNo(input.IdNo),
            input.Birthday,
            input.Name,
            input.Email,
            input.Role,
            input.IsActive);
    }

    public async Task<bool> DeactivateAsync(int year, string employeeNo, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
UPDATE teach_appo_empl_base
SET is_active = 0
WHERE yr = $yr AND empl_no = $emplNo;
""";
        command.Parameters.AddWithValue("$yr", year);
        command.Parameters.AddWithValue("$emplNo", employeeNo);

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0;
    }

    private TeacherIdentityRecord Map(SqliteDataReader reader)
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
            _maskingPolicy.MaskIdNo(idNo),
            birthday,
            name,
            email,
            role,
            isActive);
    }
}
