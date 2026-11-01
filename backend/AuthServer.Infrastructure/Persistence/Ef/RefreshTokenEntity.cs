using AuthServer.Domain.OAuth;

namespace AuthServer.Infrastructure.Persistence.Ef;

// See the domain RefreshToken for the meaning of the fields.
public class RefreshTokenEntity
{
    public Guid Id { get; set; }
    public string TokenHash { get; set; } = "";
    public Guid FamilyId { get; set; }
    public Guid ClientId { get; set; }
    public virtual ClientEntity Client { get; set; } = null!;
    public Guid UserId { get; set; }
    public virtual UserEntity User { get; set; } = null!;
    public Guid? TenantId { get; set; }
    public List<string> Scopes { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ExchangedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public static RefreshTokenEntity FromDomain(RefreshToken token) => new()
    {
        Id = token.Id,
        TokenHash = token.TokenHash,
        FamilyId = token.FamilyId,
        ClientId = token.ClientId,
        UserId = token.UserId,
        TenantId = token.TenantId,
        Scopes = [.. token.Scopes],
        CreatedAt = token.CreatedAt,
        ExpiresAt = token.ExpiresAt,
        ExchangedAt = token.ExchangedAt,
        RevokedAt = token.RevokedAt
    };

    public RefreshToken ToDomain() => new(
        id: Id,
        tokenHash: TokenHash,
        familyId: FamilyId,
        clientId: ClientId,
        userId: UserId,
        tenantId: TenantId,
        scopes: Scopes,
        createdAt: CreatedAt,
        expiresAt: ExpiresAt,
        exchangedAt: ExchangedAt,
        revokedAt: RevokedAt);
}
