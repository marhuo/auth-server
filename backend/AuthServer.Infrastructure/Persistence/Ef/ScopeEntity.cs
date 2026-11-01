using AuthServer.Domain.Scopes;

namespace AuthServer.Infrastructure.Persistence.Ef;

public class ScopeEntity : ISoftDeletableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public Guid? ResourceServerId { get; set; } // If null, this scope is global and not tied to a specific resource server
    public DateTimeOffset CreatedAt { get; set; }
    public virtual ResourceServerEntity? ResourceServer { get; set; }
    public virtual List<ClientEntity> Clients { get; set; } = [];
    public bool IsStandard { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public static ScopeEntity FromDomain(Scope scope)
    {
        var entity = new ScopeEntity
        {
            Id = scope.Id,
            IsStandard = scope.IsStandard,
            CreatedAt = scope.CreatedAt
        };
        entity.UpdateFrom(scope);
        return entity;
    }

    public void UpdateFrom(Scope scope)
    {
        Name = scope.Name;
        DisplayName = scope.DisplayName;
        ResourceServerId = scope.ResourceServerId;
        DeletedAt = scope.DeletedAt;
    }

    public Scope ToDomain() => new(
        id: Id,
        name: Name,
        displayName: DisplayName,
        resourceServerId: ResourceServerId,
        isStandard: IsStandard,
        createdAt: CreatedAt,
        deletedAt: DeletedAt);
}
