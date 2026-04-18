namespace TeacherAppointment.Api.Security;

public sealed class QrOptions
{
    public const string SectionName = "Qr";

    public string? PublicBaseUrl { get; init; }
}