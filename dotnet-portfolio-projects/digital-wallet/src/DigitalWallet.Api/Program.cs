using DigitalWallet.Api.Contracts;
using DigitalWallet.Api.Data;
using DigitalWallet.Api.Messaging;
using DigitalWallet.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WalletDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddStackExchangeRedisCache(o =>
    o.Configuration = builder.Configuration.GetConnectionString("Redis"));
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddScoped<WalletService>();
builder.Services.AddOpenApi();

var app = builder.Build();

// Demo convenience. Use EF Core migrations in a real deployment.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<WalletDbContext>().Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/wallets", async (CreateWalletRequest req, WalletService svc, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(req.Msisdn) || req.OpeningBalance < 0)
        return Results.BadRequest(new { error = "invalid_request" });

    var wallet = await svc.CreateWalletAsync(req.Msisdn, req.OpeningBalance, ct);
    return wallet is null
        ? Results.Conflict(new { error = "wallet_exists" })
        : Results.Created($"/wallets/{wallet.Id}/balance",
            new WalletResponse(wallet.Id, wallet.Msisdn, wallet.Balance, wallet.Currency));
});

app.MapGet("/wallets/{id:guid}/balance", async (Guid id, WalletService svc, CancellationToken ct) =>
{
    var balance = await svc.GetBalanceAsync(id, ct);
    return balance is null ? Results.NotFound() : Results.Ok(new { walletId = id, balance });
});

app.MapPost("/transfers", async (TransferRequest req, WalletService svc, CancellationToken ct) =>
{
    var result = await svc.TransferAsync(req, ct);
    if (result.Success) return Results.Ok(result.Transaction);

    return result.Error switch
    {
        "wallet_not_found" => Results.NotFound(new { error = result.Error }),
        "insufficient_funds" => Results.UnprocessableEntity(new { error = result.Error }),
        "concurrent_update" => Results.Conflict(new { error = result.Error }),
        _ => Results.BadRequest(new { error = result.Error })
    };
});

app.Run();
