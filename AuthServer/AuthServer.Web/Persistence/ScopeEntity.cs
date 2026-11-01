namespace AuthServer.Web.Persistence;

public class ScopeEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public Guid? ResourceServerId { get; set; } // If null, this scope is global and not tied to a specific resource server
    public virtual ResourceServerEntity? ResourceServer { get; set; }
    public virtual List<ClientEntity> Clients { get; set; } = [];
}
