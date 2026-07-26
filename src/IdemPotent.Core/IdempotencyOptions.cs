using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdemPotent.Core
{
    // Following the Options pattern, this class allows configuration of idempotency behavior in the application.
    public class IdempotencyOptions
    {
        public string HeaderName { get; set; } = "Idempotency-Key";
        public ConcurrentRequestStrategy ConcurrentRequestStrategy { get; set; } = ConcurrentRequestStrategy.Reject409;
        public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(24);
        public int MaxWaitSeconds { get; set; } = 5;
        public int PollIntervalMs { get; set; } = 200;
    }

    public enum ConcurrentRequestStrategy
    {
        Reject409,
        PollAndWait
    }
}
