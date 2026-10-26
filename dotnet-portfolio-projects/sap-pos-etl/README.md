# POS to SAP Business One ETL

A background worker that extracts daily sales from a point-of-sale database, validates and
transforms them, and creates sales orders in **SAP Business One** through the Service Layer.
Modelled on pharmacy and restaurant POS integrations.

**Stack:** .NET 9 Worker Service (`BackgroundService`), Dapper, SQL Server, SQLite, `HttpClient`, xUnit

## Pipeline
1. **Extract:** `SqlServerPosSource` reads the day's receipts and lines with Dapper.
2. **Validate:** `SalesValidator` rejects receipts with missing items, zero quantity or negative prices.
3. **Transform:** `SalesTransformer` maps a receipt to a Service Layer order and merges duplicate lines.
4. **Load:** `SapServiceLayerClient` logs in, keeps the session cookie, re-authenticates on 401 and posts to `Orders`.
5. **Sync state:** `SqliteSyncStateStore` records pushed receipts, so reruns never create duplicate orders.

A failing receipt is logged and retried on the next run without blocking the rest of the batch.

## Run
Edit `src/SapPosEtl.Worker/appsettings.json` (or use user-secrets / environment variables for credentials), then:

```bash
dotnet run --project src/SapPosEtl.Worker
```

The expected POS tables are `Sales(ReceiptId, SoldAt)` and `SaleLines(ReceiptId, ItemCode, Quantity, UnitPrice)`.

## Test
```bash
dotnet test tests/SapPosEtl.Tests
```
