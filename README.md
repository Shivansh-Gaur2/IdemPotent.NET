# IdemPotent.NET

IdemPotent.NET is a .NET library for tracking idempotent request processing.

## Projects

- `src/IdemPotent.Core` — core idempotency record and status types.
- `src/IdemPotent.Redis` — Redis-backed idempotency store.
- `src/IdemPotent.SqlServer` — SQL Server-backed idempotency store.

## Requirements

- .NET 8 SDK

## Build

```bash
dotnet build IdemPotent.sln
```

## SQL Server store

Install or reference `IdemPotent.SqlServer`, then register the store during application startup:

```csharp
builder.Services.UseSqlServerStore(
    "Server=localhost,1433;Database=IdemPotentTest;User Id=sa;Password=YourStrong@Passw0rd;Encrypt=True;TrustServerCertificate=True;");
```

The configured database must already exist. The store creates the `IdempotencyRecords` table automatically by default. To disable schema creation, configure `AutoCreateSchema` as follows:

```csharp
builder.Services.UseSqlServerStore(connectionString, options =>
{
    options.AutoCreateSchema = false;
});
```

For a local SQL Server container, create the database before starting the application:

```bash
docker run -d --name sql-local -p 1433:1433 \
  -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=YourStrong@Passw0rd" \
  mcr.microsoft.com/mssql/server:2022-latest
```

## License

See [LICENSE](LICENSE).
