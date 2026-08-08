using System.Security.Cryptography;
using System.Text;

namespace IdemShield.AspNetCore;

internal static class FingerprintCalculator
{
    public static string Compute(string method, string requestTarget, byte[] bodyBytes)
    {
        using var sha256 = SHA256.Create();
        var prefixBytes = Encoding.UTF8.GetBytes($"{method}:{requestTarget}:");
        var fingerprintInput = new byte[prefixBytes.Length + bodyBytes.Length];
        Buffer.BlockCopy(prefixBytes, 0, fingerprintInput, 0, prefixBytes.Length);
        Buffer.BlockCopy(bodyBytes, 0, fingerprintInput, prefixBytes.Length, bodyBytes.Length);
        return Convert.ToHexString(sha256.ComputeHash(fingerprintInput));
    }
}
