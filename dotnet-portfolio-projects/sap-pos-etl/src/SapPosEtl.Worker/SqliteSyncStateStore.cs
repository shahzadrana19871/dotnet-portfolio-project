using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace SapPosEtl.Worker;

/// <summary>Remembers which POS receipts were already pushed to SAP so reruns never create duplicates.</summary>
public class SqliteSyncStateStore : ISyncStateStore
{
    private readonly string _connectionString;

    public SqliteSyncStateStore(IOptions<EtlOptions> options)
    {
        _connectionString = $"Data Source={options.Value.SyncDbPath}";
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS synced_receipts (
                receipt_id TEXT PRIMARY KEY,
                doc_entry  INTEGER NOT NULL,
                synced_at  TEXT NOT NULL
            )
            """);
    }

    public async Task<bool> IsSyncedAsync(string receiptId, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM synced_receipts WHERE receipt_id = @receiptId",
            new { receiptId }, cancellationToken: ct));
        return count > 0;
    }

    public async Task MarkSyncedAsync(string receiptId, int docEntry, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT OR REPLACE INTO synced_receipts (receipt_id, doc_entry, synced_at) VALUES (@receiptId, @docEntry, @at)",
            new { receiptId, docEntry, at = DateTime.UtcNow.ToString("O") }, cancellationToken: ct));
    }
}
