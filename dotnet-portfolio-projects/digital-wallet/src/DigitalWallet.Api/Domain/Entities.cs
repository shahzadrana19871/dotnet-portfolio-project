namespace DigitalWallet.Api.Domain;

public class Wallet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Msisdn { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum TransactionStatus
{
    Completed = 1,
    Failed = 2
}

public class WalletTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Client-supplied unique key that makes transfers idempotent.</summary>
    public string Reference { get; set; } = string.Empty;
    public Guid FromWalletId { get; set; }
    public Guid ToWalletId { get; set; }
    public decimal Amount { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
