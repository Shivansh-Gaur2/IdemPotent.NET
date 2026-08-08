using IdemShield.AspNetCore;
using IdemShield.Redis;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdemShield.Redis.Tests;

public class RedisIdempotencyStoreIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Store_round_trips_and_releases_an_idempotency_record()
    {
        var connectionString = Environment.GetEnvironmentVariable("IDEMSHIELD_REDIS_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var key = $"redis-test-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.UseRedisStore(connectionString);
        await using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IIdempotencyStore>();
        var record = new IdempotencyRecord
        {
            IdempotencyKey = key,
            RequestFingerprint = "redis-integration-test",
            Status = IdempotencyStatus.InProgress,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        try
        {
            Assert.True(await store.TryInsertInProgressAsync(record, CancellationToken.None));
            Assert.False(await store.TryInsertInProgressAsync(record, CancellationToken.None));

            var inProgress = await store.GetAsync(key, CancellationToken.None);
            Assert.NotNull(inProgress);
            Assert.Equal(IdempotencyStatus.InProgress, inProgress.Status);

            await store.UpdateAsCompletedAsync(
                key,
                201,
                "{\"orderId\":42}",
                "{\"Content-Type\":\"application/json\"}",
                CancellationToken.None);

            var completed = await store.GetAsync(key, CancellationToken.None);
            Assert.NotNull(completed);
            Assert.Equal(IdempotencyStatus.Completed, completed.Status);
            Assert.Equal(201, completed.ResponseStatusCode);
            Assert.Equal("{\"orderId\":42}", completed.ResponseBody);
        }
        finally
        {
            await store.DeleteAsync(key, CancellationToken.None);
        }

        Assert.Null(await store.GetAsync(key, CancellationToken.None));
    }
}
