using IdemShield.AspNetCore;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IdemShield.Redis
{
    /// <summary>Persists idempotency records in Redis with native key expiry.</summary>
    public class RedisIdempotencyStore : IIdempotencyStore
    {
        private readonly IConnectionMultiplexer _redis;
        private const string KeyPrefix = "idempotency:";

        /// <summary>Creates a Redis-backed idempotency store.</summary>
        /// <param name="redis">The shared Redis connection multiplexer.</param>
        public RedisIdempotencyStore(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }
        /// <inheritdoc/>
        public async Task DeleteAsync(string key, CancellationToken ct)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(KeyPrefix + key);
        }

        /// <inheritdoc/>
        public async Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct)
        {
            var db = _redis.GetDatabase();
            var json = await db.StringGetAsync(KeyPrefix + key);

            if (json.IsNullOrEmpty)
            {
                return null;
            }
            return JsonSerializer.Deserialize<IdempotencyRecord>(json!);
        }

        /// <inheritdoc/>
        public async Task<bool> TryInsertInProgressAsync(IdempotencyRecord record, CancellationToken ct)
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(record);

            var ttl = record.ExpiresAt - DateTimeOffset.UtcNow;

            return await db.StringSetAsync(KeyPrefix + record.IdempotencyKey, json, ttl, When.NotExists);
        }

        /// <inheritdoc/>
        public async Task UpdateAsCompletedAsync(string key, int statusCode, string body, string headers, CancellationToken ct)
        {
            var db = _redis.GetDatabase();
            var existingJson = await db.StringGetAsync(KeyPrefix + key);

            if (existingJson.IsNullOrEmpty)
                return;

            var record = JsonSerializer.Deserialize<IdempotencyRecord>(existingJson!)!;
            record.Status = IdempotencyStatus.Completed;
            record.ResponseStatusCode = statusCode;
            record.ResponseBody = body;
            record.ResponseHeaders = headers;

            var ttl = record.ExpiresAt - DateTimeOffset.UtcNow;
            var updatedJson = JsonSerializer.Serialize(record);

            if (ttl > TimeSpan.Zero)
            {
                await db.StringSetAsync(KeyPrefix + key, updatedJson, ttl);
            }
            else
            {
                await db.KeyDeleteAsync(KeyPrefix + key);
            }
        }
    }
}
