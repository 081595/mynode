namespace TeacherAppointment.Application.Abstractions.Infrastructure;

public interface IQrCodeGenerator
{
    string GenerateDataUri(string payload);
}
