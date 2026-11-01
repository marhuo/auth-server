using AuthServer.Domain.Users;

namespace AuthServer.Infrastructure.Persistence.Ef;

public class PasswordTokenEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public PasswordTokenTypes Type { get; set; }
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }

    public static PasswordTokenEntity FromDomain(PasswordToken token)
    {
        var entity = new PasswordTokenEntity { Id = token.Id };
        entity.UpdateFrom(token);
        return entity;
    }

    public void UpdateFrom(PasswordToken token)
    {
        UserId = token.UserId;
        Type = token.Type;
        Email = token.Email;
        NormalizedEmail = token.NormalizedEmail;
        TokenHash = token.TokenHash;
        CreatedAt = token.CreatedAt;
        ExpiresAt = token.ExpiresAt;
        UsedAt = token.UsedAt;
    }

    public PasswordToken ToDomain() => new(
        id: Id,
        userId: UserId,
        type: Type,
        email: Email,
        normalizedEmail: NormalizedEmail,
        tokenHash: TokenHash,
        createdAt: CreatedAt,
        expiresAt: ExpiresAt,
        usedAt: UsedAt);
}
