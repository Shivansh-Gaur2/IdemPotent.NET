using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace IdemPotent.Core
{
    /// <summary>
    /// Configures how idempotency keys are handled by <see cref="IdempotencyMiddleware"/>.
    /// </summary>
    public class IdempotencyOptions
    {
        /// <summary>Gets or sets the request header that carries the client-generated idempotency key.</summary>
        public string HeaderName { get; set; } = "Idempotency-Key";

        /// <summary>Gets or sets how requests with an in-progress key are handled.</summary>
        public ConcurrentRequestStrategy ConcurrentRequestStrategy { get; set; } = ConcurrentRequestStrategy.Reject409;

        /// <summary>Gets or sets how long an idempotency record is retained.</summary>
        public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(24);

        /// <summary>Gets or sets the maximum time to wait for an in-progress request.</summary>
        public int MaxWaitSeconds { get; set; } = 5;

        /// <summary>Gets or sets the polling interval while waiting for an in-progress request.</summary>
        public int PollIntervalMs { get; set; } = 200;

        /// <summary>
        /// Gets or sets an optional function that derives the store key from the request and client key.
        /// Use this to scope keys by a tenant or authenticated user when the application requires it.
        /// </summary>
        public Func<HttpContext, string, string>? KeySelector { get; set; }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(HeaderName))
                throw new ArgumentException("The idempotency header name is required.", nameof(HeaderName));
            if (DefaultTtl <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(DefaultTtl), "The default TTL must be positive.");
            if (MaxWaitSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(MaxWaitSeconds), "The maximum wait must not be negative.");
            if (PollIntervalMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(PollIntervalMs), "The polling interval must be positive.");
        }
    }

    /// <summary>Defines how a request is handled while another request with the same key is in progress.</summary>
    public enum ConcurrentRequestStrategy
    {
        Reject409,
        PollAndWait
    }
}
