using System.Text;
using TeacherAppointment.Application.Abstractions.Infrastructure;

namespace TeacherAppointment.Infrastructure.Integrations.QrCodes;

public sealed class PlaceholderQrCodeGenerator : IQrCodeGenerator
{
    public string GenerateDataUri(string payload)
    {
        // 生成簡單的視覺化 QR 碼（21x21 棋盤，基於 payload 的 hash）
        using var hash = System.Security.Cryptography.SHA256.Create();
        var hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var bits = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        
        const int size = 21;
        var svg = new StringBuilder();
        
        svg.AppendLine($"<svg xmlns='http://www.w3.org/2000/svg' width='336' height='336' viewBox='0 0 {size} {size}'>");
        svg.AppendLine("<style>.q { fill: black; } .w { fill: white; }</style>");
        
        // 黑色邊框（Finder patterns 模擬）
        for (int i = 0; i < 7; i++) {
            svg.AppendLine($"<rect class='q' x='{i}' y='0' width='1' height='1'/>");
            svg.AppendLine($"<rect class='q' x='0' y='{i}' width='1' height='1'/>");
            svg.AppendLine($"<rect class='q' x='{i}' y='{size - 1}' width='1' height='1'/>");
            svg.AppendLine($"<rect class='q' x='{size - 1}' y='{i}' width='1' height='1'/>");
        }
        
        // 根據 hash 填充中間區域
        int bitIdx = 0;
        for (int y = 7; y < size - 7; y++) {
            for (int x = 7; x < size - 7; x++) {
                char bit = bits[bitIdx % bits.Length];
                bool isBlack = (bit < '8');
                svg.AppendLine($"<rect class='{(isBlack ? 'q' : 'w')}' x='{x}' y='{y}' width='1' height='1'/>");
                bitIdx++;
            }
        }
        
        svg.AppendLine("</svg>");
        
        var svgString = svg.ToString();
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(svgString));
        return $"data:image/svg+xml;base64,{base64}";
    }
}
