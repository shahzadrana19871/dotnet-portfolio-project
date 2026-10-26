using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SapPosEtl.Worker;

namespace SapPosEtl.Tests;

public class EtlTests
{
    private sealed class FakeSource(params PosSale[] sales) : IPosSalesSource
    {
        public Task<IReadOnlyList<PosSale>> GetSalesAsync(DateOnly date, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<PosSale>>(sales);
    }

    private sealed class FakeSap : ISapClient
    {
        public List<SalesOrderDto> Created { get; } = new();
        public HashSet<string> FailFor { get; } = new();

        public Task<int> CreateSalesOrderAsync(SalesOrderDto order, CancellationToken ct)
        {
            if (FailFor.Contains(order.NumAtCard)) throw new HttpRequestException("SAP unavailable");
            Created.Add(order);
            return Task.FromResult(1000 + Created.Count);
        }
    }

    private sealed class MemoryState : ISyncStateStore
    {
        public HashSet<string> Synced { get; } = new();
        public Task<bool> IsSyncedAsync(string receiptId, CancellationToken ct) => Task.FromResult(Synced.Contains(receiptId));

        public Task MarkSyncedAsync(string receiptId, int docEntry, CancellationToken ct)
        {
            Synced.Add(receiptId);
            return Task.CompletedTask;
        }
    }

    private static PosSale Sale(string id, params (string Item, decimal Qty, decimal Price)[] lines) => new()
    {
        ReceiptId = id,
        SoldAt = new DateTime(2026, 10, 1, 12, 0, 0),
        Lines = lines.Select(l => new PosSaleLine { ItemCode = l.Item, Quantity = l.Qty, UnitPrice = l.Price }).ToList()
    };

    private static EtlRunner Runner(IPosSalesSource source, FakeSap sap, MemoryState state) =>
        new(source, sap, state, Options.Create(new EtlOptions()), NullLogger<EtlRunner>.Instance);

    [Fact]
    public async Task Loads_valid_receipts_and_marks_them_synced()
    {
        var sap = new FakeSap();
        var state = new MemoryState();
        var runner = Runner(new FakeSource(Sale("R1", ("A", 2, 5m)), Sale("R2", ("B", 1, 9m))), sap, state);

        var summary = await runner.RunAsync(new DateOnly(2026, 10, 1), CancellationToken.None);

        Assert.Equal(new EtlSummary(2, 2, 0, 0, 0), summary);
        Assert.Equal(2, sap.Created.Count);
        Assert.Contains("R1", state.Synced);
    }

    [Fact]
    public async Task Skips_receipts_that_were_already_synced()
    {
        var sap = new FakeSap();
        var state = new MemoryState();
        state.Synced.Add("R1");
        var runner = Runner(new FakeSource(Sale("R1", ("A", 1, 5m))), sap, state);

        var summary = await runner.RunAsync(new DateOnly(2026, 10, 1), CancellationToken.None);

        Assert.Equal(1, summary.Skipped);
        Assert.Empty(sap.Created);
    }

    [Fact]
    public async Task Rejects_invalid_receipts_without_calling_sap()
    {
        var sap = new FakeSap();
        var runner = Runner(new FakeSource(Sale("R1", ("A", -1, 5m)), Sale("R2")), sap, new MemoryState());

        var summary = await runner.RunAsync(new DateOnly(2026, 10, 1), CancellationToken.None);

        Assert.Equal(2, summary.Invalid);
        Assert.Empty(sap.Created);
    }

    [Fact]
    public async Task One_failing_receipt_does_not_stop_the_batch()
    {
        var sap = new FakeSap();
        sap.FailFor.Add("R1");
        var state = new MemoryState();
        var runner = Runner(new FakeSource(Sale("R1", ("A", 1, 5m)), Sale("R2", ("B", 1, 9m))), sap, state);

        var summary = await runner.RunAsync(new DateOnly(2026, 10, 1), CancellationToken.None);

        Assert.Equal(1, summary.Failed);
        Assert.Equal(1, summary.Loaded);
        Assert.DoesNotContain("R1", state.Synced); // retried on the next run
        Assert.Contains("R2", state.Synced);
    }

    [Fact]
    public void Transformer_merges_duplicate_item_lines()
    {
        var sale = Sale("R1", ("A", 1, 5m), ("A", 2, 5m), ("B", 1, 7m));

        var order = SalesTransformer.ToSalesOrder(sale, new EtlOptions { DefaultCardCode = "C1", WarehouseCode = "02" });

        Assert.Equal("C1", order.CardCode);
        Assert.Equal("R1", order.NumAtCard);
        Assert.Equal("2026-10-01", order.DocDate);
        Assert.Equal(2, order.DocumentLines.Count);
        Assert.Equal(3m, order.DocumentLines.Single(l => l.ItemCode == "A").Quantity);
        Assert.All(order.DocumentLines, l => Assert.Equal("02", l.WarehouseCode));
    }
}
