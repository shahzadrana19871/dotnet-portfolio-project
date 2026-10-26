using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace SapPosEtl.Worker;

public class SqlServerPosSource(IOptions<PosOptions> options) : IPosSalesSource
{
    private sealed class Row
    {
        public string ReceiptId { get; set; } = string.Empty;
        public DateTime SoldAt { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    private const string Sql = """
        SELECT s.ReceiptId, s.SoldAt, l.ItemCode, l.Quantity, l.UnitPrice
        FROM Sales s
        JOIN SaleLines l ON l.ReceiptId = s.ReceiptId
        WHERE s.SoldAt >= @From AND s.SoldAt < @To
        ORDER BY s.ReceiptId
        """;

    public async Task<IReadOnlyList<PosSale>> GetSalesAsync(DateOnly date, CancellationToken ct)
    {
        var from = date.ToDateTime(TimeOnly.MinValue);
        await using var connection = new SqlConnection(options.Value.ConnectionString);
        var rows = await connection.QueryAsync<Row>(new CommandDefinition(
            Sql, new { From = from, To = from.AddDays(1) }, cancellationToken: ct));

        return rows
            .GroupBy(r => r.ReceiptId)
            .Select(g => new PosSale
            {
                ReceiptId = g.Key,
                SoldAt = g.First().SoldAt,
                Lines = g.Select(r => new PosSaleLine
                {
                    ItemCode = r.ItemCode,
                    Quantity = r.Quantity,
                    UnitPrice = r.UnitPrice
                }).ToList()
            })
            .ToList();
    }
}
