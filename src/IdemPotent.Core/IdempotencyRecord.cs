using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdemPotent.Core
{
    public class IdempotencyRecord
    {
        public string IdempotencyKey { get; set; } = string.Empty;
        public string RequestFingerprint { get; set; } = string.Empty;
        public IdempotencyStatus Status { get; set; }
        public int? ResponseStatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public string? ResponseHeaders { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }

    public enum IdempotencyStatus
    {
        InProgress,
        Completed
    }
}

