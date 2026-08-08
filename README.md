# IdemShield.NET

[![CI](https://github.com/Shivansh-Gaur2/IdemShield.NET/actions/workflows/ci.yml/badge.svg)](https://github.com/Shivansh-Gaur2/IdemShield.NET/actions/workflows/ci.yml)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Reliable idempotency middleware for ASP.NET Core. IdemShield lets clients safely retry `POST`, `PUT`, and `PATCH` requests without repeating completed side effects.

> **Release status:** `0.1.0` preview. The package artifacts are validated in CI but are not considered publicly released until the matching NuGet packages and GitHub release exist.

## Packages

| Package | Purpose |
|---|---|
| `IdemShield.AspNetCore` | Middleware, configuration, and storage abstractions |
| `IdemShield.Redis` | Distributed Redis-backed store with native record expiry |
| `IdemShield.SqlServer` | SQL Server store with coordinated background expiry cleanup |

All packages target .NET 8 and include symbols, XML documentation, the license expression, repository metadata, and this README.

## Install

Install the ASP.NET Core package and one provider after the `0.1.0` packages are published:

```bash
dotnet add package IdemShield.AspNetCore --version 0.1.0
dotnet add package IdemShield.SqlServer --version 0.1.0
```

For Redis, replace the second command with:

```bash
dotnet add package IdemShield.Redis --version 0.1.0
```

## 60-second setup

Register the middleware and a store before building the application:

```csharp
using IdemShield.AspNetCore;
using IdemShield.SqlServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdempotency();
builder.Services.UseSqlServerStore(
    builder.Configuration.GetConnectionString("IdemShield")!);

var app = builder.Build();
app.UseIdempotency();

app.MapPost("/orders", () => Results.Created("/orders/42", new { orderId = 42 }));
app.Run();
```

Clients retry with the same idempotency key:

```http
POST /orders HTTP/1.1
Idempotency-Key: order-2026-00042
Content-Type: application/json

{"productName":"book"}
```

The first completed response is stored. A later request with the same key and request fingerprint receives the stored response without invoking the endpoint again.

## Provider setup

### Redis

```csharp
using IdemShield.Redis;

builder.Services.UseRedisStore(
    builder.Configuration.GetConnectionString("Redis")!);
```

Redis applies each record's expiry as a native key TTL.

### SQL Server

```csharp
using IdemShield.SqlServer;

builder.Services.UseSqlServerStore(connectionString, options =>
{
    options.CleanupInterval = TimeSpan.FromMinutes(30);
    options.CleanupBatchSize = 500;
});
```

SQL Server creates the `IdempotencyRecords` table by default. Disable automatic creation when schema changes are DBA-managed:

```csharp
builder.Services.UseSqlServerStore(connectionString, options =>
{
    options.AutoCreateSchema = false;
});
```

The cleanup worker runs immediately and then at the configured interval. Multiple application instances coordinate through a SQL Server application lock so only one instance performs a cleanup batch at a time.

## Behavioral contract

- Only `POST`, `PUT`, and `PATCH` requests containing the configured header are intercepted.
- Keys must contain 1–255 characters after optional application-defined scoping.
- Reusing a key with a different HTTP method, path, query string, or body returns `422 Unprocessable Entity`.
- An in-progress request returns `409 Conflict` by default, or can poll for the completed result.
- If endpoint execution throws, the in-progress record is removed so a later retry can run.
- Completed responses replay the status code, body, content type, and safe application headers.
- The default record lifetime is 24 hours.

Scope keys for tenant-aware or user-aware APIs:

```csharp
builder.Services.AddIdempotency(options =>
{
    options.KeySelector = (context, clientKey) =>
        $"{context.User.FindFirst("tenant_id")?.Value}:{clientKey}";
});
```

## Operational considerations

- IdemShield buffers request and response bodies in memory and replays response bodies as UTF-8 text. Set application-level size limits and do not use it for binary or streaming responses.
- Do not place secrets or personal data directly in idempotency keys.
- SQL automatic schema creation requires DDL permission during application startup. Disable it where deployment tooling owns schema changes.
- Store availability is part of the request path. Apply normal Redis or SQL Server resilience, monitoring, and capacity practices.
- Idempotency prevents duplicate execution only for requests routed through this middleware and sharing the same backing store and key scope.

See the focused documentation:

- [Architecture](docs/architecture.md)
- [Configuration reference](docs/configuration.md)
- [Provider guide](docs/providers.md)
- [Operations and failure behavior](docs/operations.md)
- [Release process](docs/releasing.md)
- [Versioning policy](docs/versioning.md)

## Project health

Every push and pull request builds the solution in Release mode, runs unit and real-provider integration tests, creates all three NuGet packages, validates package contents, audits dependencies, and compiles a clean consumer application from the generated packages.

Security reports should follow [SECURITY.md](SECURITY.md). For support and compatibility expectations, see [SUPPORT.md](SUPPORT.md).

## Contributing

Contributions are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) and the [Code of Conduct](CODE_OF_CONDUCT.md) before opening a pull request.

## License

IdemShield.NET is licensed under the [MIT License](LICENSE).
