using NewtonsoftJson = Newtonsoft.Json;

namespace JuiceShop.DataPipeline.Models;

public sealed class PartnerIngestRequest
{
    [NewtonsoftJson.JsonProperty("latestFilePath")]
    public string? LatestFilePath { get; set; }
}
