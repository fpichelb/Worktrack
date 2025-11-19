using QRCoder;
using System.Drawing;

namespace Worktrack.Services;

public static class QrHelper
{
    public static Task<string> GeneratePngBase64(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        using var code = new QRCode(data);
        using var bmp = code.GetGraphic(20);

        using var ms = new MemoryStream();
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        var base64 = $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";

        return Task.FromResult(base64);
    }
}
