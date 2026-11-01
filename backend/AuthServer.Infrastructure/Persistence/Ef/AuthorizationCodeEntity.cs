using AuthServer.Domain.OAuth;

namespace AuthServer.Infrastructure.Persistence.Ef;

public class AuthorizationCodeEntity
{
    public Guid Id { get; set; }
    public string CodeHash { get; set; } = "";
    public Guid ClientId { get; set; }
    public virtual ClientEntity Client { get; set; } = null!;
    public Guid UserId { get; set; }
    public virtual UserEntity User { get; set; } = null!;
    public Guid? TenantId { get; set; }
    public string RedirectUri { get; set; } = "";
    public List<string> Scopes { get; set; } = [];
    public string CodeChallenge { get; set; } = "";
    public string? Nonce { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }

    public static AuthorizationCodeEntity FromDomain(AuthorizationCode code) => new()
    {
        Id = code.Id,
        CodeHash = code.CodeHash,
        ClientId = code.ClientId,
        UserId = code.UserId,
        TenantId = code.TenantId,
        RedirectUri = code.RedirectUri,
        Scopes = [.. code.Scopes],
        CodeChallenge = code.CodeChallenge,
        Nonce = code.Nonce,
        CreatedAt = code.CreatedAt,
        ExpiresAt = code.ExpiresAt,
        UsedAt = code.UsedAt
    };

    public AuthorizationCode ToDomain() => new(
        id: Id,
        codeHash: CodeHash,
        clientId: ClientId,
        userId: UserId,
        tenantId: TenantId,
        redirectUri: RedirectUri,
        scopes: Scopes,
        codeChallenge: CodeChallenge,
        nonce: Nonce,
        createdAt: CreatedAt,
        expiresAt: ExpiresAt,
        usedAt: UsedAt);
}
