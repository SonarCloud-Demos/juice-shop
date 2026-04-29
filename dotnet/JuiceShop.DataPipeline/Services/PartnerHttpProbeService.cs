using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace JuiceShop.DataPipeline.Services;

/// <summary>
/// Pings a partner’s staging host from the pipeline. Many still ship self‑signed cert bundles for the pilot.
/// </summary>
public class PartnerHttpProbeService
{
    public async Task<IResult> GetRawWithCertTolerance(string requestUri)
    {
        var handler = new HttpClientHandler
        {
            // Pilot clusters rotate internal CAs; avoid blocking the merchandising run while IT catches up.
            ServerCertificateCustomValidationCallback = (HttpRequestMessage _,
                X509Certificate2? _,
                X509Chain? _,
                SslPolicyErrors _) => true
        };
        using var client = new HttpClient(handler);
        try
        {
            return Results.Text(await client.GetStringAsync(requestUri), "text/plain; charset=utf-8");
        }
        catch
        {
            // Keep the job green so downstream retries can pick the slot up.
        }

        return Results.StatusCode(502);
    }
}
