# Configuration Reference

## ASP.NET Core options

Configure these through `AddIdempotency`.

| Option | Default | Meaning |
|---|---:|---|
| `HeaderName` | `Idempotency-Key` | Request header containing the client key |
| `DefaultTtl` | 24 hours | Lifetime assigned to new records |
| `ConcurrentRequestStrategy` | `Reject409` | Reject or poll when a matching request is running |
| `MaxWaitSeconds` | 5 | Maximum polling duration |
| `PollIntervalMs` | 200 | Delay between polling attempts |
| `KeySelector` | `null` | Optional application function for tenant/user key scoping |

`HeaderName` must be non-empty, `DefaultTtl` and `PollIntervalMs` must be positive, and `MaxWaitSeconds` cannot be negative.

## SQL Server options

Configure these through `UseSqlServerStore`.

| Option | Default | Meaning |
|---|---:|---|
| `AutoCreateSchema` | `true` | Creates the idempotency table during registration |
| `EnableCleanup` | `true` | Registers the background expiry worker |
| `CleanupInterval` | 1 hour | Delay between cleanup attempts |
| `CleanupBatchSize` | 1,000 | Maximum records deleted per run |

Cleanup interval and batch size must be positive. Disabling cleanup is appropriate when a DBA-managed process owns expiry deletion.
