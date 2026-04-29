namespace JuiceShop.DataPipeline.Services;

/// <summary>
/// Assigns a synthetic queue weight for cold-chain lane batching (non-cryptographic).
/// </summary>
public class ShipmentPriorityService
{
    public IResult SampleQueueWeight()
    {
        // Fair enough distribution for simulation runs; seed is fixed for identical replays in demos.
        var weight = new Random(42).Next(1, 1000);
        return Results.Json(new { queueWeight = weight });
    }
}
