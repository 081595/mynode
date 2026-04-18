using QRCoder;
using TeacherAppointment.Application.Abstractions.Infrastructure;

namespace TeacherAppointment.Infrastructure.Integrations.QrCodes;

public sealed class PlaceholderQrCodeGenerator : IQrCodeGenerator
{
    public string GenerateDataUri(string payload)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(qrData);
        var pngBytes = pngQr.GetGraphic(20);
        var base64 = Convert.ToBase64String(pngBytes);
        return $"data:image/png;base64,{base64}";
    }
}
