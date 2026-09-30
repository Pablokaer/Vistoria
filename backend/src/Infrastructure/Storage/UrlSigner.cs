using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace InspectFlow.Infrastructure.Storage;

/// <summary>HMAC-SHA256 signatures for expiring file URLs (the local equivalent of S3 presigned URLs).</summary>
public sealed class UrlSigner(IOptions<StorageOptions> options)
{
    private byte[] Key => Encoding.UTF8.GetBytes(options.Value.SigningKey);

    public string Sign(string key, long expiresUnix)
    {
        var mac = HMACSHA256.HashData(Key, Encoding.UTF8.GetBytes($"{key}|{expiresUnix}"));
        return Convert.ToBase64String(mac).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public bool IsValid(string key, long expiresUnix, string? signature, DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(signature) || expiresUnix < now.ToUnixTimeSeconds()) return false;
        var expected = Encoding.ASCII.GetBytes(Sign(key, expiresUnix));
        var actual = Encoding.ASCII.GetBytes(signature);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
