using Formarca.Application.Common.Exceptions;

namespace Formarca.Application.Contracts;

internal static class PngSignature
{
    private static readonly byte[] Header = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private const int MaxBytes = 512_000;

    public static byte[] DecodeRequired(string? signaturePngBase64)
    {
        if (string.IsNullOrWhiteSpace(signaturePngBase64))
        {
            throw new ConflictException("Client signature is required.");
        }

        var raw = signaturePngBase64.Trim();
        var comma = raw.IndexOf(',');
        if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
        {
            raw = raw[(comma + 1)..];
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(raw);
        }
        catch (FormatException)
        {
            throw new ConflictException("Client signature must be a PNG image.");
        }

        if (bytes.Length > MaxBytes)
        {
            throw new ConflictException("Client signature image is too large.");
        }

        if (bytes.Length < Header.Length || !bytes.AsSpan(0, Header.Length).SequenceEqual(Header))
        {
            throw new ConflictException("Client signature must be a PNG image.");
        }

        return bytes;
    }
}
