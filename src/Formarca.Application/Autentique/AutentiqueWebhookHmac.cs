using System.Security.Cryptography;
using System.Text;

namespace Formarca.Application.Autentique;

public static class AutentiqueWebhookHmac
{
    public static bool Verify(string rawBody, string? signatureHeader, string? secret)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signatureHeader) || rawBody is null)
        {
            return false;
        }

        var expected = ComputeHex(rawBody, secret);
        var provided = signatureHeader.Trim().ToLowerInvariant();
        if (provided.Length != expected.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(provided));
    }

    public static string ComputeHex(string rawBody, string secret)
    {
        ArgumentNullException.ThrowIfNull(rawBody);
        ArgumentException.ThrowIfNullOrEmpty(secret);
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(rawBody));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
