using System.Text;
using TeacherAppointment.Application.Abstractions.Infrastructure;

namespace TeacherAppointment.Infrastructure.Integrations.QrCodes;

public sealed class PlaceholderQrCodeGenerator : IQrCodeGenerator
{
    public string GenerateDataUri(string payload)
    {
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
        return $"data:text/plain;base64,{base64}";
    }
}
