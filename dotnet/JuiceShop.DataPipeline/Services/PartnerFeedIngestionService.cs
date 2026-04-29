using JuiceShop.DataPipeline.Models;
using NewtonsoftJson = Newtonsoft.Json;

namespace JuiceShop.DataPipeline.Services;

/// <summary>
/// Parses JSON metadata from the connector (drop folder path) and provides fast size metrics
/// to confirm the partner file landed and looks reasonable before the scheduled load.
/// </summary>
public class PartnerFeedIngestionService
{
    public IResult SummarizePartnerDropFromBody(TextReader bodyReader)
    {
        var body = bodyReader.ReadToEnd();
        var request = NewtonsoftJson.JsonConvert.DeserializeObject<PartnerIngestRequest>(body);
        if (request is null || string.IsNullOrWhiteSpace(request.LatestFilePath))
        {
            return Results.BadRequest("LatestFilePath is required.");
        }

        var filePath = request.LatestFilePath;
        if (!File.Exists(filePath))
        {
            return Results.NotFound("Partner file was not found at the given path.");
        }

        var raw = File.ReadAllBytes(filePath);
        return Results.Json(new
        {
            sizeBytes = raw.Length,
            sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(raw))
        });
    }
}
