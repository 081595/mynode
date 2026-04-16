using System.ComponentModel.DataAnnotations;

namespace TeacherAppointment.Infrastructure.Persistence;

public sealed class SqliteOptions
{
    public const string SectionName = "Sqlite";

    [Required]
    public string ConnectionString { get; init; } = "Data Source=./data/teacher-appointment.db";
}
