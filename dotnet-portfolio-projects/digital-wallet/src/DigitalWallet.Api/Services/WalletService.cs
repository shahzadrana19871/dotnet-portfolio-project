using System.Globalization;
using DigitalWallet.Api.Contracts;
using DigitalWallet.Api.Data;
using DigitalWallet.Api.Domain;
using DigitalWallet.Api.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace DigitalWallet.Api.Services;

public record TransferResult(bool Success, string? Error, WalletTransaction? Transaction)
{
    public static TransferResult Ok(WalletTransaction tx) => new(true, null, tx);
    public static TransferResult Fail(string error) => new(false, error, null);
}

public class WalletService(
    WalletDbContext db,
    IDistributedCache cache,
    IEventPublisher publisher,
    ILogger<WalletService> logger)
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan BalanceCacheTtl = TimeSpan.FromSeconds(30);

    private static string BalanceKey(Guid id) => $"wallet:balance:{id}";

    public async Task<Wallet?> CreateWalletAsync(string msisdn, decimal openingBalance, CancellationToken ct = default)
    {
        if (await db.Wallets.AnyAsync(w => w.Msisdn == msisdn, ct)) return null;

        var wallet = new Wallet { Msisdn = msisdn, Balance = openingBalance };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync(ct);
        return wallet;
    }

    /// <summary>Reads the balance through a short-lived Redis cache.</summary>
    public async Task<decimal?> GetBalanceAsync(Guid walletId, CancellationToken ct = default)
    {
        var key = BalanceKey(walletId);
        var cached = await cache.GetStringAsync(key, ct);
        if (cached is not null &&
            decimal.TryParse(cached, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        var balance = await db.Wallets.AsNoTracking()
            .Where(w => w.Id == walletId)
            .Select(w => (decimal?)w.Balance)
            .FirstOrDefaultAsync(ct);

        if (balance is not null)
        {
            await cache.SetStringAsync(
                key,
                balance.Value.ToString(CultureInfo.InvariantCulture),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = BalanceCacheTtl },
                ct);
        }

        return balance;
    }

    /// <summary>
    /// Moves money between two wallets. Idempotent on <see cref="TransferRequest.Reference"/>
    /// and protected against concurrent updates with optimistic concurrency + retry.
    /// </summary>
    public async Task<TransferResult> TransferAsync(TransferRequest request, CancellationToken ct = default)
    {
        if (request.Amount <= 0) return TransferResult.Fail("invalid_amount");
        if (request.FromWalletId == request.ToWalletId) return TransferResult.Fail("same_wallet");
        if (string.IsNullOrWhiteSpace(request.Reference)) return TransferResult.Fail("reference_required");

        var existing = await db.Transactions.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Reference == request.Reference, ct);
        if (existing is not null) return TransferResult.Ok(existing);

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            var from = await db.Wallets.FirstOrDefaultAsync(w => w.Id == request.FromWalletId, ct);
            var to = await db.Wallets.FirstOrDefaultAsync(w => w.Id == request.ToWalletId, ct);

            if (from is null || to is null) return TransferResult.Fail("wallet_not_found");
            if (from.Currency != to.Currency) return TransferResult.Fail("currency_mismatch");
            if (from.Balance < request.Amount) return TransferResult.Fail("insufficient_funds");

            from.Balance -= request.Amount;
            to.Balance += request.Amount;

            var tx = new WalletTransaction
            {
                Reference = request.Reference,
                FromWalletId = from.Id,
                ToWalletId = to.Id,
                Amount = request.Amount,
                Status = TransactionStatus.Completed
            };
            db.Transactions.Add(tx);

            try
            {
                // One SaveChanges = one database transaction: both balances and the ledger row commit together.
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogWarning("Concurrent update on transfer {Reference}, attempt {Attempt}", request.Reference, attempt);
                db.ChangeTracker.Clear();
                continue;
            }
            catch (DbUpdateException)
            {
                // Lost a race on the unique Reference index: return the winner's result.
                db.ChangeTracker.Clear();
                var winner = await db.Transactions.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Reference == request.Reference, ct);
                if (winner is not null) return TransferResult.Ok(winner);
                throw;
            }

            await cache.RemoveAsync(BalanceKey(from.Id), ct);
            await cache.RemoveAsync(BalanceKey(to.Id), ct);
            await TryPublishAsync(tx, ct);
            return TransferResult.Ok(tx);
        }

        return TransferResult.Fail("concurrent_update");
    }

    // Production systems should use the transactional outbox pattern here so the event
    // can never be lost between the database commit and the broker publish.
    private async Task TryPublishAsync(WalletTransaction tx, CancellationToken ct)
    {
        try
        {
            await publisher.PublishAsync(
                "wallet.transfer.completed",
                new TransferCompleted(tx.Id, tx.FromWalletId, tx.ToWalletId, tx.Amount, tx.Reference, tx.CreatedAt),
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to publish transfer event for {Reference}", tx.Reference);
        }
    }
}
