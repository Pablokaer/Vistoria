namespace InspectFlow.Infrastructure.Identity;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "inspectflow";
    public string Audience { get; set; } = "inspectflow-clients";

    /// <summary>HMAC-SHA256 key, at least 32 bytes. Supplied via JWT_SECRET; never committed.</summary>
    public string Secret { get; set; } = string.Empty;
}
