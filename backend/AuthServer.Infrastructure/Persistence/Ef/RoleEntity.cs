using AuthServer.Domain.Roles;

namespace AuthServer.Infrastructure.Persistence.Ef;

public class RoleEntity : ISoftDeletableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public virtual List<UserRoleEntity> UserRoles { get; set; } = [];
    public Guid ResourceServerId { get; set; }
    public virtual ResourceServerEntity ResourceServer { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public static RoleEntity FromDomain(Role role)
    {
        var entity = new RoleEntity { Id = role.Id, CreatedAt = role.CreatedAt };
        entity.UpdateFrom(role);
        return entity;
    }

    public void UpdateFrom(Role role)
    {
        Name = role.Name;
        Description = role.Description;
        ResourceServerId = role.ResourceServerId;
        DeletedAt = role.DeletedAt;
    }

    public Role ToDomain() => new(
        id: Id,
        resourceServerId: ResourceServerId,
        name: Name,
        description: Description,
        createdAt: CreatedAt,
        deletedAt: DeletedAt);
}
