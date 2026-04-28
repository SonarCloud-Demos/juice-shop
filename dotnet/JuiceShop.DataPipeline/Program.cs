using System.Text;
using JuiceShop.DataPipeline.Services;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);

var sqlite = new SqliteConnection("Data Source=orders-pipeline;Mode=Memory;Cache=Shared");
sqlite.Open();
using (var init = sqlite.CreateCommand())
{
    init.CommandText = "CREATE TABLE orders (id INT PRIMARY KEY, product TEXT, total REAL); " +
        "INSERT INTO orders VALUES (1, 'Apple Juice (330ml)', 1.99), (2, 'Raspberry', 2.50);";
    init.ExecuteNonQuery();
}

builder.Services.AddSingleton(_ => sqlite);
builder.Services.AddSingleton<PartnerFeedIngestionService>();
builder.Services.AddSingleton<SupportOrderLookupService>();
builder.Services.AddHttpClient<MerchandisingFeedPreviewService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Batch reconciliation: read partner EDI/CSV drops for quick sanity metrics before the nightly job runs.
app.MapPost("/api/pipeline/ingest/summary", (HttpRequest request, PartnerFeedIngestionService pipeline) =>
{
    using var reader = new StreamReader(request.Body, Encoding.UTF8);
    return pipeline.SummarizePartnerDropFromBody(reader);
});

// Support / ops: run flexible lookups against the warehouse snapshot without pushing a new release.
app.MapGet("/api/warehouse/orders/lookup", (string whereFragment, SupportOrderLookupService support) =>
    support.RunTicketLookup(whereFragment));

// Merchandising: verify an upstream catalog URL responds before we schedule the sync window.
app.MapGet("/api/merch/feed-peek", async (string sourceUrl, MerchandisingFeedPreviewService merch) =>
    await merch.VerifyVendorFeedResponse(sourceUrl));

app.MapGet("/healthz", () => Results.Ok("ok"));

app.Run();
