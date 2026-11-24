using AuthServer.Domain.ResourceServers;

namespace AuthServer.Infrastructure.Persistence.Ef;

public class ResourceServerEntity
{
    public Guid Id { get; set; }
    public string Audience { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public virtual List<ScopeEntity> Scopes { get; set; } = [];
    public virtual List<RoleEntity> Roles { get; set; } = [];
    public virtual List<ClientEntity> Clients { get; set; } = [];
    public bool IsAuthServer { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public static ResourceServerEntity FromDomain(ResourceServer resourceServer)
    {
        var entity = new ResourceServerEntity
        {
            Id = resourceServer.Id,
            IsAuthServer = resourceServer.IsAuthServer,
            CreatedAt = resourceServer.CreatedAt
        };
        entity.UpdateFrom(resourceServer);
        return entity;
    }

    public void UpdateFrom(ResourceServer resourceServer)
    {
        Audience = resourceServer.Audience;
        DisplayName = resourceServer.DisplayName;
    }

    public ResourceServer ToDomain() => new(
        id: Id,
        audience: Audience,
        displayName: DisplayName,
        isAuthServer: IsAuthServer,
        createdAt: CreatedAt);
}
