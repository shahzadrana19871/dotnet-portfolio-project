# Digital Wallet API

A compact reference implementation of the core of a mobile-money / digital-wallet backend
(the kind of system behind money transfers, bill payments and agent cash-in/cash-out).

**Stack:** .NET 9, ASP.NET Core minimal APIs, EF Core + PostgreSQL, Redis, RabbitMQ, Docker, xUnit

## What it demonstrates
- **Idempotent transfers** via a unique client `Reference`, so retries never double-spend.
- **Optimistic concurrency** (PostgreSQL `xmin`) with bounded retries.
- **Atomic ledger writes**: both balances and the transaction row commit in one `SaveChanges`.
- **Redis read-through cache** for balances, invalidated on every transfer.
- **Domain events** published to a RabbitMQ topic exchange (`wallet.transfer.completed`).
- **Unit tests** for the money-movement rules.

## Run
```bash
docker compose up --build
```

```bash
# create two wallets (opening balance is a demo-only shortcut)
curl -X POST localhost:8080/wallets -H 'content-type: application/json' \
  -d '{"msisdn":"+261340000001","openingBalance":100}'
curl -X POST localhost:8080/wallets -H 'content-type: application/json' \
  -d '{"msisdn":"+261340000002"}'

# transfer
curl -X POST localhost:8080/transfers -H 'content-type: application/json' \
  -d '{"fromWalletId":"<id-1>","toWalletId":"<id-2>","amount":25,"reference":"order-1001"}'

curl localhost:8080/wallets/<id-2>/balance
```

## Test
```bash
dotnet test tests/DigitalWallet.Tests
```

## Production notes
Use EF Core migrations instead of `EnsureCreated`, add JWT authentication and rate limiting,
and replace the direct publish with a transactional outbox.
