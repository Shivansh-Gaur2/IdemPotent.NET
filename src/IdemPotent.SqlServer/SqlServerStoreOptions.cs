namespace IdemPotent.SqlServer;

public class SqlServerStoreOptions
{
    public bool AutoCreateSchema { get; set; } = true;
    public bool EnableCleanup { get; set; } = true;
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
    public int CleanupBatchSize { get; set; } = 1000;

    internal void Validate()
    {
        if (CleanupInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(CleanupInterval), "Cleanup interval must be positive.");
        }

        if (CleanupBatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(CleanupBatchSize), "Cleanup batch size must be positive.");
        }
    }
}
