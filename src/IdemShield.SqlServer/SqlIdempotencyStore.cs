using IdemShield.AspNetCore;
using Microsoft.Data.SqlClient;

namespace IdemShield.SqlServer;

/// <summary>Persists idempotency records in SQL Server.</summary>
public class SqlIdempotencyStore : IIdempotencyStore
{
    private readonly string _connectionString;

    /// <summary>Creates a SQL Server-backed idempotency store.</summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    public SqlIdempotencyStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc/>
    public async Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        const string sql = "SELECT IdempotencyKey, RequestFingerprint, Status, ResponseStatusCode, ResponseBody, ResponseHeaders, CreatedAt, ExpiresAt FROM IdempotencyRecords WHERE IdempotencyKey = @key AND ExpiresAt > SYSDATETIMEOFFSET()";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@key", key);

        await using var reader = await command.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
            return null;

        return new IdempotencyRecord
        {
            IdempotencyKey = reader.GetString(0),
            RequestFingerprint = reader.GetString(1),
            Status = (IdempotencyStatus)reader.GetByte(2),
            ResponseStatusCode = reader.IsDBNull(3) ? null : reader.GetInt32(3),
            ResponseBody = reader.IsDBNull(4) ? null : reader.GetString(4),
            ResponseHeaders = reader.IsDBNull(5) ? null : reader.GetString(5),
            CreatedAt = reader.GetDateTimeOffset(6),
            ExpiresAt = reader.GetDateTimeOffset(7)
        };
    }

    /// <inheritdoc/>
    public async Task<bool> TryInsertInProgressAsync(IdempotencyRecord record, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        const string sql = "DELETE FROM IdempotencyRecords WHERE IdempotencyKey = @key AND ExpiresAt <= SYSDATETIMEOFFSET(); INSERT INTO IdempotencyRecords (IdempotencyKey, RequestFingerprint, Status, CreatedAt, ExpiresAt) VALUES (@key, @fingerprint, @status, @createdAt, @expiresAt)";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@key", record.IdempotencyKey);
        command.Parameters.AddWithValue("@fingerprint", record.RequestFingerprint);
        command.Parameters.AddWithValue("@status", (byte)record.Status);
        command.Parameters.AddWithValue("@createdAt", record.CreatedAt);
        command.Parameters.AddWithValue("@expiresAt", record.ExpiresAt);

        try
        {
            await command.ExecuteNonQueryAsync(ct);
            return true;
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task UpdateAsCompletedAsync(string key, int statusCode, string body, string headers, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        const string sql = "UPDATE IdempotencyRecords SET Status = @status, ResponseStatusCode = @statusCode, ResponseBody = @body, ResponseHeaders = @headers WHERE IdempotencyKey = @key";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@status", (byte)IdempotencyStatus.Completed);
        command.Parameters.AddWithValue("@statusCode", statusCode);
        command.Parameters.AddWithValue("@body", body);
        command.Parameters.AddWithValue("@headers", headers);
        command.Parameters.AddWithValue("@key", key);

        await command.ExecuteNonQueryAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string key, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        const string sql = "DELETE FROM IdempotencyRecords WHERE IdempotencyKey = @key";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@key", key);

        await command.ExecuteNonQueryAsync(ct);
    }
}
