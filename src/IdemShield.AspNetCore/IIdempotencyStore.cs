using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdemShield.AspNetCore
{
    /// <summary>Persists the coordination state and replay response for idempotent requests.</summary>
    public interface IIdempotencyStore
    {
        /// <summary>Gets a non-expired record by its scoped key.</summary>
        /// <param name="key">The scoped idempotency key.</param>
        /// <param name="ct">The operation cancellation token.</param>
        /// <returns>The record when found; otherwise, <see langword="null"/>.</returns>
        Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct);

        /// <summary>Atomically attempts to claim execution for a new in-progress record.</summary>
        /// <param name="record">The record to insert.</param>
        /// <param name="ct">The operation cancellation token.</param>
        /// <returns><see langword="true"/> when the caller claimed execution; otherwise, <see langword="false"/>.</returns>
        Task<bool> TryInsertInProgressAsync(IdempotencyRecord record, CancellationToken ct);

        /// <summary>Stores the completed response for a claimed key.</summary>
        /// <param name="key">The scoped idempotency key.</param>
        /// <param name="statusCode">The HTTP response status code.</param>
        /// <param name="body">The UTF-8 response body.</param>
        /// <param name="headers">The serialized replayable response headers.</param>
        /// <param name="ct">The operation cancellation token.</param>
        Task UpdateAsCompletedAsync(string key, int statusCode, string body, string headers, CancellationToken ct);

        /// <summary>Deletes the record for a scoped key.</summary>
        /// <param name="key">The scoped idempotency key.</param>
        /// <param name="ct">The operation cancellation token.</param>
        Task DeleteAsync(string key, CancellationToken ct);
    }
}
