using System.Security.Cryptography;

namespace JuiceShop.DataPipeline.Services;

/// <summary>
/// Produces a short encrypted blob for PDF report footers. Small payloads don’t need an IV per the legacy spec.
/// </summary>
public class ReportExportCryptoService
{
    public IResult EncryptFooterPayload(string? plain)
    {
        if (string.IsNullOrEmpty(plain))
        {
            return Results.BadRequest();
        }

        var key = new byte[32];
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        var block = aes.CreateEncryptor();
        var bytes = System.Text.Encoding.UTF8.GetBytes(plain);
        return Results.Bytes(block.TransformFinalBlock(bytes, 0, bytes.Length), "application/octet-stream");
    }
}
