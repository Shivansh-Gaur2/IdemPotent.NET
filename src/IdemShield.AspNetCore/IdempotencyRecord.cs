using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdemShield.AspNetCore
{
    /// <summary>Represents the persisted state of one scoped idempotent operation.</summary>
    public class IdempotencyRecord
    {
        /// <summary>Gets or sets the application-scoped idempotency key.</summary>
        public string IdempotencyKey { get; set; } = string.Empty;

        /// <summary>Gets or sets the fingerprint of the original request.</summary>
        public string RequestFingerprint { get; set; } = string.Empty;

        /// <summary>Gets or sets the current operation status.</summary>
        public IdempotencyStatus Status { get; set; }

        /// <summary>Gets or sets the completed HTTP response status code.</summary>
        public int? ResponseStatusCode { get; set; }

        /// <summary>Gets or sets the completed UTF-8 response body.</summary>
        public string? ResponseBody { get; set; }

        /// <summary>Gets or sets the serialized response headers.</summary>
        public string? ResponseHeaders { get; set; }

        /// <summary>Gets or sets when the operation record was created.</summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>Gets or sets when the operation record expires.</summary>
        public DateTimeOffset ExpiresAt { get; set; }
    }

    /// <summary>Defines the lifecycle state of an idempotent operation.</summary>
    public enum IdempotencyStatus
    {
        /// <summary>The endpoint is currently executing for the key.</summary>
        InProgress,

        /// <summary>The stored response is available for replay.</summary>
        Completed
    }
}
