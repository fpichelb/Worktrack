using QRCoder;
using System.Drawing;

namespace Worktrack.Services;

public static class QrHelper
{
    public static Task<string> GeneratePngBase64(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);

        var qrCode = new PngByteQRCode(data);
        byte[] bytes = qrCode.GetGraphic(20); // 20 = Größe (kannst du anpassen)

        var base64 = Convert.ToBase64String(bytes);

        // Wenn du das direkt im <img>-Tag verwenden willst:
        var dataUrl = $"data:image/png;base64,{base64}";

        return Task.FromResult(dataUrl);
    }
}
