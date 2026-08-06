# IdemPotent.NET

IdemPotent prevents duplicate side effects in ASP.NET Core APIs. Send the same `Idempotency-Key` with the same `POST`, `PUT`, or `PATCH` request and the middleware returns the original saved response instead of executing the endpoint twice.

## Packages

- `IdemPotent.Core` — middleware and storage abstraction.
- `IdemPotent.Redis` — Redis implementation for distributed deployments.
- `IdemPotent.SqlServer` — SQL Server implementation with automatic expiry cleanup.

All packages target .NET 8.

## Quick start

Install `IdemPotent.Core` and one storage provider:

```bash
dotnet add package IdemPotent.Core
dotnet add package IdemPotent.SqlServer
```

Register idempotency and your chosen store before building the application:

```csharp
using IdemPotent.Core;
using IdemPotent.SqlServer;

builder.Services.AddIdempotency();
builder.Services.UseSqlServerStore(builder.Configuration.GetConnectionString("Idempotency")!);

var app = builder.Build();
app.UseIdempotency();
```

Clients include an idempotency key on supported requests:

```http
POST /orders
Idempotency-Key: order-123
Content-Type: application/json

{"productName":"book"}
```

## Store configuration

Redis:

```csharp
using IdemPotent.Redis;

builder.Services.UseRedisStore("localhost:6379");
```

SQL Server creates the `IdempotencyRecords` table by default. Disable automatic schema creation when a DBA manages schema changes:

```csharp
builder.Services.UseSqlServerStore(connectionString, options =>
{
    options.AutoCreateSchema = false;
});
```

SQL Server also removes expired records in the background. The default is an immediate run at startup, then every hour in batches of 1,000. In a multi-instance deployment, SQL Server ensures only one instance cleans at a time.

```csharp
builder.Services.UseSqlServerStore(connectionString, options =>
{
    options.CleanupInterval = TimeSpan.FromMinutes(30);
    options.CleanupBatchSize = 500;
});
```

## License

Licensed under the [MIT License](LICENSE).
