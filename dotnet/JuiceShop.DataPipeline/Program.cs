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
builder.Services.AddSingleton<InventoryTextSearchService>();
builder.Services.AddSingleton<LegacyPartnerAuthService>();
builder.Services.AddSingleton<PartnerHttpProbeService>();
builder.Services.AddSingleton<ReportExportCryptoService>();
builder.Services.AddSingleton<AutomationBridgeService>();
builder.Services.AddSingleton<ShipmentPriorityService>();

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

// Ops: ad hoc search against the latest snapshot lines embedded in the microservice.
var snapshotLines = new[] { "SKU-1 Orange", "SKU-2 Apple Juice 1L", "Pallet-88 Raspberry" };
app.MapGet("/api/warehouse/inventory/snapshot-search", (string? pattern, InventoryTextSearchService search) =>
    search.FindMatchesInSnapshot(pattern, snapshotLines));

// Backwards-compatible fingerprint for a subset of on-prem kiosks.
app.MapGet("/api/legacy/partner-fingerprint", (string? body, LegacyPartnerAuthService auth) =>
    auth.VerifyLegacyFingerprint(body));

// Pilot partner endpoints using private CAs.
app.MapGet("/api/partner/http-peek-tolerant", async (string requestUri, PartnerHttpProbeService probe) =>
    await probe.GetRawWithCertTolerance(requestUri));

// Reports: footer blob for PDF exports.
app.MapGet("/api/reports/encrypt-footer", (string? text, ReportExportCryptoService report) =>
    report.EncryptFooterPayload(text));

// ETL: bridge to legacy .cmd on the Windows jump host.
app.MapGet("/api/ops/etl-bridge", (string? scriptPath, string? args, AutomationBridgeService bridge) =>
    bridge.RunScriptOnBridge(scriptPath, args));

// Simulation: non-prod load shaping for the cold-chain batcher.
app.MapGet("/api/shipments/sim/queue-weight", (ShipmentPriorityService s) => s.SampleQueueWeight());

app.MapGet("/healthz", () => Results.Ok("ok"));

app.Run();
