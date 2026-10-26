using DigitalWallet.Api.Contracts;
using DigitalWallet.Api.Data;
using DigitalWallet.Api.Messaging;
using DigitalWallet.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DigitalWallet.Tests;

public class WalletServiceTests
{
    private sealed class FakePublisher : IEventPublisher
    {
        public List<(string RoutingKey, object Message)> Published { get; } = new();

        public Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default)
        {
            Published.Add((routingKey, message!));
            return Task.CompletedTask;
        }
    }

    private static (WalletService Service, FakePublisher Publisher) Create()
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new WalletDbContext(options);
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var publisher = new FakePublisher();
        return (new WalletService(db, cache, publisher, NullLogger<WalletService>.Instance), publisher);
    }

    [Fact]
    public async Task Transfer_moves_funds_and_publishes_event()
    {
        var (svc, publisher) = Create();
        var a = (await svc.CreateWalletAsync("+261340000001", 100m))!;
        var b = (await svc.CreateWalletAsync("+261340000002", 0m))!;

        var result = await svc.TransferAsync(new TransferRequest(a.Id, b.Id, 40m, "ref-1"));

        Assert.True(result.Success);
        Assert.Equal(60m, await svc.GetBalanceAsync(a.Id));
        Assert.Equal(40m, await svc.GetBalanceAsync(b.Id));
        Assert.Single(publisher.Published);
        Assert.Equal("wallet.transfer.completed", publisher.Published[0].RoutingKey);
    }

    [Fact]
    public async Task Transfer_rejects_insufficient_funds()
    {
        var (svc, publisher) = Create();
        var a = (await svc.CreateWalletAsync("+261340000001", 10m))!;
        var b = (await svc.CreateWalletAsync("+261340000002", 0m))!;

        var result = await svc.TransferAsync(new TransferRequest(a.Id, b.Id, 50m, "ref-2"));

        Assert.False(result.Success);
        Assert.Equal("insufficient_funds", result.Error);
        Assert.Equal(10m, await svc.GetBalanceAsync(a.Id));
        Assert.Empty(publisher.Published);
    }

    [Fact]
    public async Task Transfer_is_idempotent_for_the_same_reference()
    {
        var (svc, publisher) = Create();
        var a = (await svc.CreateWalletAsync("+261340000001", 100m))!;
        var b = (await svc.CreateWalletAsync("+261340000002", 0m))!;
        var request = new TransferRequest(a.Id, b.Id, 25m, "ref-3");

        var first = await svc.TransferAsync(request);
        var second = await svc.TransferAsync(request);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(first.Transaction!.Id, second.Transaction!.Id);
        Assert.Equal(75m, await svc.GetBalanceAsync(a.Id));
        Assert.Single(publisher.Published);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Transfer_rejects_non_positive_amounts(int amount)
    {
        var (svc, _) = Create();
        var a = (await svc.CreateWalletAsync("+261340000001", 100m))!;
        var b = (await svc.CreateWalletAsync("+261340000002", 0m))!;

        var result = await svc.TransferAsync(new TransferRequest(a.Id, b.Id, amount, "ref-4"));

        Assert.Equal("invalid_amount", result.Error);
    }

    [Fact]
    public async Task CreateWallet_returns_null_for_duplicate_msisdn()
    {
        var (svc, _) = Create();
        await svc.CreateWalletAsync("+261340000001", 0m);

        Assert.Null(await svc.CreateWalletAsync("+261340000001", 0m));
    }
}
