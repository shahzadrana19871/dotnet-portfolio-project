namespace SapPosEtl.Worker;

public class PosSaleLine
{
    public string ItemCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class PosSale
{
    public string ReceiptId { get; set; } = string.Empty;
    public DateTime SoldAt { get; set; }
    public List<PosSaleLine> Lines { get; set; } = new();
}

/// <summary>Payload shape expected by the SAP Business One Service Layer "Orders" endpoint.</summary>
public class SalesOrderLineDto
{
    public string ItemCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? WarehouseCode { get; set; }
}

public class SalesOrderDto
{
    public string CardCode { get; set; } = string.Empty;
    public string DocDate { get; set; } = string.Empty;
    public string DocDueDate { get; set; } = string.Empty;
    public string NumAtCard { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public List<SalesOrderLineDto> DocumentLines { get; set; } = new();
}

public record EtlSummary(int Extracted, int Loaded, int Skipped, int Invalid, int Failed);
