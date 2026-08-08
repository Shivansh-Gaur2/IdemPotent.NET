using IdemPotent.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace IdemPotent.SqlServer.Tests;

public class SqlExpiredRecordCleanupIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Cleanup_removes_expired_records_without_removing_active_records()
    {
        var connectionString = Environment.GetEnvironmentVariable("IDEMPOTENT_SQL_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var prefix = $"cleanup-test-{Guid.NewGuid():N}";
        var expiredKey = $"{prefix}-expired";
        var activeKey = $"{prefix}-active";
        var services = new ServiceCollection();
        services.UseSqlServerStore(connectionString, options =>
        {
            options.CleanupInterval = TimeSpan.FromHours(1);
            options.CleanupBatchSize = 100;
        });
        await using var provider = services.BuildServiceProvider();
        var cleanupService = Assert.Single(provider.GetServices<IHostedService>());

        try
        {
            await InsertRecordAsync(connectionString, expiredKey, DateTimeOffset.UtcNow.AddMinutes(-1));
            await InsertRecordAsync(connectionString, activeKey, DateTimeOffset.UtcNow.AddHours(1));

            await cleanupService.StartAsync(CancellationToken.None);
            try
            {
                await WaitForDeletionAsync(connectionString, expiredKey);
            }
            finally
            {
                await cleanupService.StopAsync(CancellationToken.None);
            }

            Assert.Equal(0, await CountRecordsAsync(connectionString, expiredKey));
            Assert.Equal(1, await CountRecordsAsync(connectionString, activeKey));
        }
        finally
        {
            await DeleteRecordsAsync(connectionString, prefix);
        }
    }

    private static async Task InsertRecordAsync(string connectionString, string key, DateTimeOffset expiresAt)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "INSERT INTO IdempotencyRecords (IdempotencyKey, RequestFingerprint, Status, CreatedAt, ExpiresAt) VALUES (@key, @fingerprint, @status, @createdAt, @expiresAt)",
            connection);
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@fingerprint", "cleanup-test");
        command.Parameters.AddWithValue("@status", 0);
        command.Parameters.AddWithValue("@createdAt", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("@expiresAt", expiresAt);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task WaitForDeletionAsync(string connectionString, string key)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (await CountRecordsAsync(connectionString, key) == 0)
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("The expired test record was not deleted by the cleanup service.");
    }

    private static async Task<int> CountRecordsAsync(string connectionString, string key)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT COUNT(*) FROM IdempotencyRecords WHERE IdempotencyKey = @key", connection);
        command.Parameters.AddWithValue("@key", key);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task DeleteRecordsAsync(string? connectionString, string prefix)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("DELETE FROM IdempotencyRecords WHERE IdempotencyKey LIKE @prefix", connection);
        command.Parameters.AddWithValue("@prefix", $"{prefix}%");
        await command.ExecuteNonQueryAsync();
    }
}
