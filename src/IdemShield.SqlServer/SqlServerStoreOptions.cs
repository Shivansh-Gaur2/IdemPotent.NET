namespace IdemShield.SqlServer;

/// <summary>Configures schema ownership and expiry cleanup for the SQL Server provider.</summary>
public class SqlServerStoreOptions
{
    /// <summary>Gets or sets whether registration creates the required table and index.</summary>
    public bool AutoCreateSchema { get; set; } = true;

    /// <summary>Gets or sets whether the background expiry cleanup worker is registered.</summary>
    public bool EnableCleanup { get; set; } = true;

    /// <summary>Gets or sets the delay between cleanup attempts.</summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Gets or sets the maximum expired records deleted by one cleanup run.</summary>
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
