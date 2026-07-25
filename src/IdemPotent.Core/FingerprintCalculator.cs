using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IdemPotent.Core
{
    public static class FingerprintCalculator
    {
        public static string Compute(string method, string path, byte[] bodyBytes)
        {
            using var sha256 = SHA256.Create();

            var combined = $"{method}:{path}:";

            //standard way to convert the string to bytes
            var combinedBytes = Encoding.UTF8.GetBytes(combined);

            // combined the bytes finally by making sure that I combine method(Post/Put): Path
            var allBytes = combinedBytes.Concat(bodyBytes).ToArray();

            var hashBytes = sha256.ComputeHash(allBytes);

            // ToHexString would convert these hash bytes to human-readable text
            return Convert.ToHexString(hashBytes);
        }
    }
}
