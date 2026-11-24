using AuthServer.Domain.Tenants;

namespace AuthServer.Infrastructure.Persistence.Ef;

public class TenantDomainEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = "";
    public string VerificationCode { get; set; } = "";
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public static TenantDomainEntity FromDomain(TenantDomain domain)
    {
        var entity = new TenantDomainEntity { Id = domain.Id, CreatedAt = domain.CreatedAt };
        entity.UpdateFrom(domain);
        return entity;
    }

    public void UpdateFrom(TenantDomain domain)
    {
        TenantId = domain.TenantId;
        Name = domain.Name;
        VerificationCode = domain.VerificationCode;
        VerifiedAt = domain.VerifiedAt;
    }

    public TenantDomain ToDomain() => new(
        id: Id,
        tenantId: TenantId,
        name: Name,
        verificationCode: VerificationCode,
        verifiedAt: VerifiedAt,
        createdAt: CreatedAt);
}
