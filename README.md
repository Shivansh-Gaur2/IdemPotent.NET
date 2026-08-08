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

The key must be unique for the operation and may be up to 255 characters. Reusing a key with a different method, path, or request body returns `422 Unprocessable Entity`; a request that is already in progress returns `409 Conflict` by default.

For multi-tenant or user-specific APIs, scope the client key to the caller. The library leaves this opt-in because authentication models differ between applications:

```csharp
builder.Services.AddIdempotency(options =>
{
    options.KeySelector = (context, key) =>
        $"{context.User.FindFirst("tenant_id")?.Value}:{key}";
});
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

## Behavior

- Only `POST`, `PUT`, and `PATCH` requests with the configured header are handled.
- Responses are replayed with their saved status code, body, content type, and application response headers.
- The default record TTL is 24 hours. Redis expires records natively; SQL Server ignores expired records immediately and cleans them up in the background.

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
