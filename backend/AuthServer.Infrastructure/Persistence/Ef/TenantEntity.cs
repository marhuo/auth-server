using AuthServer.Domain.Tenants;

namespace AuthServer.Infrastructure.Persistence.Ef;

public class TenantEntity : ISoftDeletableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    /// <summary>
    /// Short url friendly name
    /// </summary>
    public string Slug { get; set; } = "";
    public DateTimeOffset? SuspendedAt { get; set; }
    public string? SuspensionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public virtual List<UserRoleEntity> UserRoles { get; set; } = [];
    public virtual List<ClientEntity> Clients { get; set; } = [];

    public static TenantEntity FromDomain(Tenant tenant)
    {
        var entity = new TenantEntity { Id = tenant.Id, CreatedAt = tenant.CreatedAt };
        entity.UpdateFrom(tenant);
        return entity;
    }

    public void UpdateFrom(Tenant tenant)
    {
        Name = tenant.Name;
        NormalizedName = tenant.NormalizedName;
        Slug = tenant.Slug;
        SuspendedAt = tenant.SuspendedAt;
        SuspensionReason = tenant.SuspensionReason;
        DeletedAt = tenant.DeletedAt;
    }

    public Tenant ToDomain() => new(
        id: Id,
        name: Name,
        normalizedName: NormalizedName,
        slug: Slug,
        suspendedAt: SuspendedAt,
        suspensionReason: SuspensionReason,
        createdAt: CreatedAt,
        deletedAt: DeletedAt);
}
