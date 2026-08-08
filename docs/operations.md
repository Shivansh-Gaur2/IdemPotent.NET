# Operations and Failure Behavior

## Monitoring

Monitor provider connectivity, request latency, `409` and `422` response rates, SQL cleanup errors, expired-record volume, and backing-store capacity. A sudden increase in conflicts may indicate client key reuse or slow endpoint execution.

## Failure behavior

- If endpoint execution throws, IdemShield attempts to remove the in-progress record and rethrows the original failure.
- If the store is unavailable, the request cannot safely establish or replay idempotency and fails through the provider exception path.
- SQL cleanup failures are logged and retried at the next interval; they do not stop the hosted application.
- Cancellation follows `HttpContext.RequestAborted` for request-path store operations.

## Capacity

Request and response bodies are buffered in memory, and response bodies are replayed as UTF-8 text. Apply ASP.NET Core request limits and avoid using IdemShield for unbounded payloads, binary responses, or streaming responses. Size Redis and SQL Server for expected key cardinality, response size, and retention duration.

## Key design

Keys should be opaque, stable for one logical operation, and scoped to the correct tenant or user. Do not reuse a key across unrelated operations. Do not embed secrets or personal data in a key.
