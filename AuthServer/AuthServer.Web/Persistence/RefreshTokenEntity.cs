namespace AuthServer.Web.Persistence;

public class RefreshTokenEntity
{
    public Guid Id { get; set; }

    /// <summary>SHA-256 hash of the token. The token itself is never stored.</summary>
    public string TokenHash { get; set; } = "";

    /// <summary>
    /// Shared by all tokens rotated from one sign-in; reuse of an exchanged token revokes the whole family.
    /// </summary>
    public Guid FamilyId { get; set; }

    public Guid ClientId { get; set; }
    public virtual ClientEntity Client { get; set; } = null!;

    public Guid UserId { get; set; }
    public virtual UserEntity User { get; set; } = null!;

    public Guid TenantId { get; set; }

    /// <summary>Scopes granted at sign-in. A refresh may request the same or fewer.</summary>
    public List<string> Scopes { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>The token is not valid at or after this time.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>When the token was exchanged for a new one. Null if not yet exchanged.</summary>
    public DateTimeOffset? ExchangedAt { get; set; }

    /// <summary>When the token was blocked before expiry. Null if still valid.</summary>
    public DateTimeOffset? RevokedAt { get; set; }
}