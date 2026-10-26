namespace SapPosEtl.Worker;

public static class SalesTransformer
{
    /// <summary>Maps a POS receipt to a SAP sales order, merging duplicate item/price lines.</summary>
    public static SalesOrderDto ToSalesOrder(PosSale sale, EtlOptions options)
    {
        var date = sale.SoldAt.ToString("yyyy-MM-dd");

        return new SalesOrderDto
        {
            CardCode = options.DefaultCardCode,
            DocDate = date,
            DocDueDate = date,
            NumAtCard = sale.ReceiptId,
            Comments = $"POS receipt {sale.ReceiptId}",
            DocumentLines = sale.Lines
                .GroupBy(l => new { l.ItemCode, l.UnitPrice })
                .Select(g => new SalesOrderLineDto
                {
                    ItemCode = g.Key.ItemCode,
                    UnitPrice = g.Key.UnitPrice,
                    Quantity = g.Sum(x => x.Quantity),
                    WarehouseCode = options.WarehouseCode
                })
                .ToList()
        };
    }
}
