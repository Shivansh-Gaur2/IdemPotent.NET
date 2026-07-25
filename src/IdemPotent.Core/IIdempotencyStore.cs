using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdemPotent.Core
{
    public interface IIdempotencyStore
    {
        Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct);
        // checks our concurrency handling is possible 
        Task<bool> TryInsertInProgressAync(IdempotencyRecord record, CancellationToken ct);
        Task UpdateAsCompletedAsync(string key, int statusCode, string body, string headers, CancellationToken ct);
        Task DeleteAsync(string key, CancellationToken ct);
    }
}
