# MediCubes: Hospital Management (Pharmacy module)

A focused slice of a hospital management system: pharmacy inventory with stock alerts.
The full product covers HR, Finance, Inventory, Pharmacy and Operating Theater modules;
this repo shows the module pattern end to end.

**Stack:** ASP.NET Core MVC (.NET 9), Razor, EF Core + SQL Server, Bootstrap, xUnit

## Features
- Medicine catalogue with search by name or SKU
- Low-stock filter and expired / low-stock / OK status badges
- Create and edit with server-side validation and unique SKU enforcement
- Seed data on first run

## Run
Start SQL Server (for example with Docker):

```bash
docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Your_password123' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
dotnet run --project src/MediCubes.Web
```

## Test
```bash
dotnet test tests/MediCubes.Tests
```
