# .NET Portfolio Projects

Reference implementations of the kinds of systems I build, written from scratch as clean,
runnable examples. They are **sanitized demonstrations of the architecture and techniques**,
not copies of any employer or client code.

| Project | What it shows | Stack |
|---|---|---|
| [digital-wallet](digital-wallet) | Mobile-money core: idempotent transfers, optimistic concurrency, Redis caching, RabbitMQ events | .NET 9, EF Core, PostgreSQL, Redis, RabbitMQ, Docker |
| [sap-pos-etl](sap-pos-etl) | POS to SAP Business One ETL worker with validation, retries and sync state | .NET 9 Worker, Dapper, SQL Server, SQLite, SAP Service Layer |
| [medicubes-hms](medicubes-hms) | Hospital management pharmacy module with stock alerts | ASP.NET Core MVC, EF Core, SQL Server, Bootstrap |

Each project has its own README and an xUnit test project. CI (GitHub Actions) builds and tests all three.

## Quick start
```bash
dotnet test digital-wallet/tests/DigitalWallet.Tests
dotnet test sap-pos-etl/tests/SapPosEtl.Tests
dotnet test medicubes-hms/tests/MediCubes.Tests
```

## Author
Shahzad Rana | Senior .NET Developer | [LinkedIn](https://www.linkedin.com/in/shahzad-rana-28a607445)
