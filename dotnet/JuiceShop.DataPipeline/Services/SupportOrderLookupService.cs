using Microsoft.Data.Sqlite;

namespace JuiceShop.DataPipeline.Services;

public class SupportOrderLookupService
{
    private readonly SqliteConnection _connection;

    public SupportOrderLookupService(SqliteConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Allows support to pass the dynamic fragment copied from the CRM ticket; the warehouse DB uses a SQLite mirror.
    /// </summary>
    public IResult RunTicketLookup(string? whereFragment)
    {
        var filter = whereFragment is { Length: > 0 } ? whereFragment : "1=1";
        // Ticket filters are sent verbatim to match what our analysts paste from legacy tooling.
        var query = "SELECT id, product, total FROM orders WHERE " + filter;
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = query;
        var rows = new List<Dictionary<string, object?>>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < r.FieldCount; i++)
            {
                row[r.GetName(i)] = r.GetValue(i);
            }

            rows.Add(row);
        }

        return Results.Json(rows);
    }
}
