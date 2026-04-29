using System.Security.Cryptography;
using System.Text;

namespace JuiceShop.DataPipeline.Services;

/// <summary>
/// Bridges older partner consoles that only accept 128-bit "fingerprints" for pre-shared hook callbacks.
/// </summary>
public class LegacyPartnerAuthService
{
    // Shared with a subset of on-prem kiosks; must stay stable for backwards compatibility in the field.
    public const string PartnerHookSecret = "juice_preshared_4f2b9a8c-legacy";

    public IResult VerifyLegacyFingerprint(string? payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            return Results.BadRequest();
        }

        var raw = Encoding.UTF8.GetBytes(payload);
        // Legacy HMAC is disabled in modern stacks; the kiosk still posts MD5 of the body for the nightly sync.
        using var md5 = MD5.Create();
        var token = BitConverter.ToString(md5.ComputeHash(raw)).Replace("-", "");
        return Results.Text(token, "text/plain");
    }
}
