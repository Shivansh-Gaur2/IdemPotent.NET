using System.Collections.Concurrent;
using IdemPotent.Core;

namespace IdemPotent.Core.Tests;

internal sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, IdempotencyRecord> _records = new();
    private readonly TaskCompletionSource _inProgressRecordRead = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task InProgressRecordRead => _inProgressRecordRead.Task;

    public Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct)
    {
        _records.TryGetValue(key, out var record);
        if (record?.Status == IdempotencyStatus.InProgress)
        {
            _inProgressRecordRead.TrySetResult();
        }

        return Task.FromResult(record);
    }

    public Task<bool> TryInsertInProgressAsync(IdempotencyRecord record, CancellationToken ct) =>
        Task.FromResult(_records.TryAdd(record.IdempotencyKey, record));

    public Task UpdateAsCompletedAsync(string key, int statusCode, string body, string headers, CancellationToken ct)
    {
        var record = _records[key];
        record.Status = IdempotencyStatus.Completed;
        record.ResponseStatusCode = statusCode;
        record.ResponseBody = body;
        record.ResponseHeaders = headers;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        _records.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
