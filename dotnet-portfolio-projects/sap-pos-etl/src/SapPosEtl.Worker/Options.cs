namespace SapPosEtl.Worker;

public class EtlOptions
{
    public int IntervalMinutes { get; set; } = 15;
    public string DefaultCardCode { get; set; } = "C_WALKIN";
    public string WarehouseCode { get; set; } = "01";
    public string SyncDbPath { get; set; } = "sync-state.db";
}

public class PosOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class SapOptions
{
    /// <summary>Must end with a slash, e.g. https://sap-host:50000/b1s/v1/</summary>
    public string BaseUrl { get; set; } = string.Empty;
    public string CompanyDb { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
