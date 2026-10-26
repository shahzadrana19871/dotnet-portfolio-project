using Microsoft.Extensions.Options;

namespace SapPosEtl.Worker;

/// <summary>Extract (POS) -> Validate -> Transform -> Load (SAP B1), with per-receipt idempotency.</summary>
public class EtlRunner(
    IPosSalesSource source,
    ISapClient sap,
    ISyncStateStore state,
    IOptions<EtlOptions> options,
    ILogger<EtlRunner> logger)
{
    public async Task<EtlSummary> RunAsync(DateOnly date, CancellationToken ct)
    {
        var sales = await source.GetSalesAsync(date, ct);
        int loaded = 0, skipped = 0, invalid = 0, failed = 0;

        foreach (var sale in sales)
        {
            ct.ThrowIfCancellationRequested();

            if (await state.IsSyncedAsync(sale.ReceiptId, ct))
            {
                skipped++;
                continue;
            }

            var errors = SalesValidator.Validate(sale);
            if (errors.Count > 0)
            {
                invalid++;
                logger.LogWarning("Receipt {ReceiptId} rejected: {Errors}", sale.ReceiptId, string.Join(" ", errors));
                continue;
            }

            try
            {
                var order = SalesTransformer.ToSalesOrder(sale, options.Value);
                var docEntry = await sap.CreateSalesOrderAsync(order, ct);
                await state.MarkSyncedAsync(sale.ReceiptId, docEntry, ct);
                loaded++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One bad receipt must not stop the batch; it stays unsynced and is retried next run.
                failed++;
                logger.LogError(ex, "Failed to load receipt {ReceiptId}", sale.ReceiptId);
            }
        }

        return new EtlSummary(sales.Count, loaded, skipped, invalid, failed);
    }
}
