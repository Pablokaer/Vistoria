namespace InspectFlow.Modules.Identity.Domain;

/// <summary>
/// Opaque refresh token. Only the SHA-256 hash is stored. Tokens rotate on every use;
/// re-use of a rotated token revokes the whole family (theft detection).
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FamilyId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string? CreatedByIp { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
