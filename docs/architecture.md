# Architecture

IdemShield places one middleware component between supported ASP.NET Core requests and the endpoint. Storage providers implement the same public `IIdempotencyStore` contract.

## Request flow

1. Requests without the configured key, or using an unsupported HTTP method, pass through unchanged.
2. The middleware validates and optionally scopes the client key.
3. It fingerprints the HTTP method, path, query string, and buffered request body.
4. It reads the shared store for an existing record.
5. A completed matching record is replayed. A mismatched fingerprint returns `422`. An in-progress record follows the configured concurrency strategy.
6. For a new key, the store atomically attempts to create an in-progress record.
7. The endpoint runs only for the caller that creates that record.
8. A successful endpoint invocation stores the response. An exception removes the in-progress record.

## Modules

- `IdemShield.AspNetCore` owns request selection, fingerprinting, concurrency behavior, replay, configuration, and the provider contract.
- `IdemShield.Redis` owns Redis serialization, atomic insertion, expiry, and provider registration.
- `IdemShield.SqlServer` owns SQL persistence, schema bootstrap, and coordinated expiry cleanup.

Applications select one provider. All application instances serving the same idempotency scope must use the same backing store.

## Consistency boundary

IdemShield coordinates HTTP request execution through an idempotency record. It does not automatically make an endpoint's database writes and the idempotency record one atomic transaction. Applications with stronger transactional requirements should design the endpoint and storage boundary accordingly.
