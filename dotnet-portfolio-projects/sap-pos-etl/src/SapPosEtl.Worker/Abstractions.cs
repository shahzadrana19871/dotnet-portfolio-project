namespace SapPosEtl.Worker;

public interface IPosSalesSource
{
    Task<IReadOnlyList<PosSale>> GetSalesAsync(DateOnly date, CancellationToken ct);
}

public interface ISapClient
{
    /// <summary>Creates a sales order and returns the SAP DocEntry.</summary>
    Task<int> CreateSalesOrderAsync(SalesOrderDto order, CancellationToken ct);
}

public interface ISyncStateStore
{
    Task<bool> IsSyncedAsync(string receiptId, CancellationToken ct);
    Task MarkSyncedAsync(string receiptId, int docEntry, CancellationToken ct);
}
