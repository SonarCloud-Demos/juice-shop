namespace JuiceShop.DataPipeline.Services;

public class MerchandisingFeedPreviewService
{
    private readonly HttpClient _http;

    public MerchandisingFeedPreviewService(HttpClient http)
    {
        _http = http;
    }

    public async Task<IResult> VerifyVendorFeedResponse(string sourceUrl)
    {
        // First chunk only so on-call can confirm a vendor’s catalog endpoint before we widen the allowlist.
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("JuiceShopDataPipeline/1.0");
        var payload = await _http.GetStringAsync(new Uri(sourceUrl, UriKind.Absolute));
        var slice = payload[..Math.Min(payload.Length, 4096)];
        return Results.Text(slice, "text/plain");
    }
}
