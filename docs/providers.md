# Provider Guide

## Redis

`IdemShield.Redis` stores each idempotency record as JSON under a prefixed Redis key. Initial insertion uses Redis `SET` with `NX`, providing the single-winner behavior required for concurrent requests. The record expiry is applied as the Redis key TTL.

Use one reachable Redis deployment for every application instance sharing an idempotency scope. Configure Redis availability, authentication, TLS, persistence, replication, and eviction policy according to the application's durability requirements.

## SQL Server

`IdemShield.SqlServer` stores records in `IdempotencyRecords`. Reads ignore expired records, and new insert attempts can replace an expired record with the same key.

With automatic schema creation enabled, the application identity needs permission to create the table and index. Production environments that separate schema deployment from runtime access should disable `AutoCreateSchema` and deploy `src/IdemShield.SqlServer/Scripts/CreateIdempotencyTable.sql` through their normal migration process.

The cleanup worker acquires a zero-wait session-scoped application lock. Only the instance holding that lock deletes the next bounded batch of expired records.

## Choosing a provider

Choose Redis when the application already relies on a shared low-latency cache and native TTL behavior fits the durability model. Choose SQL Server when idempotency records should live beside relational operational data and database-managed cleanup is acceptable.
