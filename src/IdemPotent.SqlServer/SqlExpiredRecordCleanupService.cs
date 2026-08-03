using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IdemPotent.SqlServer;

internal sealed class SqlExpiredRecordCleanupService : BackgroundService
{
    private readonly SqlExpiredRecordCleanup _cleanup;
    private readonly TimeSpan _interval;
    private readonly ILogger<SqlExpiredRecordCleanupService> _logger;

    public SqlExpiredRecordCleanupService(
        SqlExpiredRecordCleanup cleanup,
        SqlServerStoreOptions options,
        ILogger<SqlExpiredRecordCleanupService> logger)
    {
        _cleanup = cleanup;
        _interval = options.CleanupInterval;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var deletedRecordCount = await _cleanup.DeleteExpiredAsync(stoppingToken);
                if (deletedRecordCount > 0)
                {
                    _logger.LogInformation("Deleted {DeletedRecordCount} expired idempotency records from SQL Server.", deletedRecordCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to clean up expired SQL Server idempotency records.");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
