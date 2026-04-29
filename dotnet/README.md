# Data pipeline companion

Optional ASP.NET service used by operations and support next to the main shop: it mirrors a slice of the warehouse database for ad hoc lookups, validates partner file drops before the nightly ETL run, and lets merchandising spot-check external catalog endpoints.

Run locally (after installing the [.NET SDK](https://dotnet.microsoft.com/download)):

```bash
cd JuiceShop.DataPipeline
dotnet run
```

The API listens on the URL shown in the console (see `Properties/launchSettings.json` for ports). `GET /healthz` returns a simple readiness probe.

This component is not started by the default `npm start` workflow; deploy or run it separately when you need the pipeline endpoints.
