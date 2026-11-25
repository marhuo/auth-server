using AuthServer.Domain.Users;

namespace AuthServer.Infrastructure.Persistence.Ef;

public class SignInCodeEntity
{
    public Guid Id { get; set; }
    public string NormalizedEmail { get; set; } = "";
    public string CodeHash { get; set; } = "";
    public int FailedAttempts { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }

    public static SignInCodeEntity FromDomain(SignInCode code)
    {
        var entity = new SignInCodeEntity { Id = code.Id };
        entity.UpdateFrom(code);
        return entity;
    }

    public void UpdateFrom(SignInCode code)
    {
        NormalizedEmail = code.NormalizedEmail;
        CodeHash = code.CodeHash;
        FailedAttempts = code.FailedAttempts;
        CreatedAt = code.CreatedAt;
        ExpiresAt = code.ExpiresAt;
        UsedAt = code.UsedAt;
    }

    public SignInCode ToDomain() => new(
        id: Id,
        normalizedEmail: NormalizedEmail,
        codeHash: CodeHash,
        failedAttempts: FailedAttempts,
        createdAt: CreatedAt,
        expiresAt: ExpiresAt,
        usedAt: UsedAt);
}
