namespace AuthServer.Web.Persistence;

public class AuthorizationCodeEntity
{
    public Guid Id { get; set; }
    public string CodeHash { get; set; } = "";
    public Guid ClientId { get; set; }
    public virtual ClientEntity Client { get; set; } = null!;
    public Guid UserId { get; set; }
    public virtual UserEntity User { get; set; } = null!;
    public Guid TenantId { get; set; }
    public string RedirectUri { get; set; } = "";
    public List<string> Scopes { get; set; } = [];
    public string CodeChallenge { get; set; } = "";
    public string? Nonce { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}