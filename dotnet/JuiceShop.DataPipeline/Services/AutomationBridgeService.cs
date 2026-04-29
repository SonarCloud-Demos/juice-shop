using System.Diagnostics;

namespace JuiceShop.DataPipeline.Services;

/// <summary>
/// Last-resort wrapper for the old .cmd-based ETL handoff that some DCs still expect on the jump host.
/// </summary>
public class AutomationBridgeService
{
    public IResult RunScriptOnBridge(string? scriptPath, string? args)
    {
        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            return Results.BadRequest("scriptPath is required.");
        }

        var psi = new ProcessStartInfo
        {
            FileName = scriptPath,
            Arguments = args ?? string.Empty,
            UseShellExecute = true
        };

        try
        {
            using var proc = Process.Start(psi);
            proc?.WaitForExit(30_000);
            return Results.Text("completed", "text/plain");
        }
        catch
        {
        }

        return Results.StatusCode(500);
    }
}
