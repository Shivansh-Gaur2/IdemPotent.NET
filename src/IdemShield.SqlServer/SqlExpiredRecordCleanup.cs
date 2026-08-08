using Microsoft.Data.SqlClient;

namespace IdemShield.SqlServer;

internal sealed class SqlExpiredRecordCleanup
{
    private const string CleanupLockResource = "IdemShield.SqlServer.ExpiredRecordCleanup";
    private readonly string _connectionString;
    private readonly int _batchSize;

    public SqlExpiredRecordCleanup(string connectionString, int batchSize)
    {
        _connectionString = connectionString;
        _batchSize = batchSize;
    }

    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var lockCommand = new SqlCommand(
            "DECLARE @result INT; EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0; SELECT @result;",
            connection);
        lockCommand.Parameters.AddWithValue("@resource", CleanupLockResource);

        var lockResult = Convert.ToInt32(await lockCommand.ExecuteScalarAsync(cancellationToken));
        if (lockResult < 0)
        {
            return 0;
        }

        await using var deleteCommand = new SqlCommand(
            "DELETE TOP (@batchSize) FROM IdempotencyRecords WHERE ExpiresAt <= SYSDATETIMEOFFSET();",
            connection);
        deleteCommand.Parameters.AddWithValue("@batchSize", _batchSize);

        return await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
