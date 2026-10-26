namespace DigitalWallet.Api.Contracts;

public record CreateWalletRequest(string Msisdn, decimal OpeningBalance = 0m);

public record WalletResponse(Guid Id, string Msisdn, decimal Balance, string Currency);

public record TransferRequest(Guid FromWalletId, Guid ToWalletId, decimal Amount, string Reference);

public record TransferCompleted(
    Guid TransactionId,
    Guid FromWalletId,
    Guid ToWalletId,
    decimal Amount,
    string Reference,
    DateTime CompletedAt);
