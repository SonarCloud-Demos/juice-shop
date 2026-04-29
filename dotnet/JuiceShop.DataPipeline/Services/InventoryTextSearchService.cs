using System.Text.RegularExpressions;

namespace JuiceShop.DataPipeline.Services;

/// <summary>
/// Text search for ops when catalog exports are not indexed yet. Supports a per-request pattern for one-off triage.
/// </summary>
public class InventoryTextSearchService
{
    public IResult FindMatchesInSnapshot(string? pattern, IReadOnlyList<string> sampleLines)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return Results.Json(Array.Empty<string>());
        }

        // Assemble a permissive line matcher so the team can paste a vendor substring directly from email.
        var re = new Regex(".*" + pattern + ".*", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
        var hit = new List<string>();
        foreach (var line in sampleLines)
        {
            if (re.IsMatch(line))
            {
                hit.Add(line);
            }
        }

        return Results.Json(hit);
    }
}
