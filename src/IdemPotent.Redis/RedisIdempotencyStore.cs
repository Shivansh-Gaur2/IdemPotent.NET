using IdemPotent.Core;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IdemPotent.Redis
{
    public class RedisIdempotencyStore : IIdempotencyStore
    {
        private readonly IConnectionMultiplexer _redis;
        private const string KeyPrefix = "idempotency:";

        public RedisIdempotencyStore(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }
        public async Task DeleteAsync(string key, CancellationToken ct)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(KeyPrefix + key);
        }

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

        public async Task<bool> TryInsertInProgressAync(IdempotencyRecord record, CancellationToken ct)
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(record);

            var ttl = record.ExpiresAt - DateTimeOffset.UtcNow;

            return await db.StringSetAsync(KeyPrefix + record.IdempotencyKey, json, ttl, When.NotExists);
        }

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
                await db.StringSetAsync(KeyPrefix + key, updatedJson);
            }
        }
    }
}
