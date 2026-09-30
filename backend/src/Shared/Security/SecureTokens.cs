using System.Security.Cryptography;
using System.Text;

namespace InspectFlow.Shared.Security;

public static class SecureTokens
{
    /// <summary>Cryptographically random URL-safe token (default 32 bytes = 256 bits).</summary>
    public static string Create(int bytes = 32)
    {
        var buffer = RandomNumberGenerator.GetBytes(bytes);
        return Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Numeric code with uniform distribution (e.g. 6-digit access code).</summary>
    public static string CreateNumericCode(int digits = 6)
    {
        var max = (int)Math.Pow(10, digits);
        return RandomNumberGenerator.GetInt32(0, max).ToString(new string('0', digits), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// SHA-256 hex digest. Appropriate for high-entropy secrets (random tokens) only.
    /// Low-entropy secrets (passwords, 6-digit codes) must use a slow salted hash.
    /// </summary>
    public static string Sha256Hex(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(hash);
    }

    public static string Sha256Hex(ReadOnlySpan<byte> data) => Convert.ToHexStringLower(SHA256.HashData(data));

    public static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
