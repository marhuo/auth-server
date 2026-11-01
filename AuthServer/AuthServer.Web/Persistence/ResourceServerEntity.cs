namespace AuthServer.Web.Persistence;

public class ResourceServerEntity
{
    public Guid Id { get; set; }
    public string Audience { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public virtual List<ScopeEntity> Scopes { get; set; } = [];
    public virtual List<RoleEntity> Roles { get; set; } = [];
    public virtual List<ClientEntity> Clients { get; set; } = [];
    public virtual List<PlanEntity> Plans { get; set; } = [];
    public virtual List<SubscriptionEntity> Subscriptions { get; set; } = [];
    public bool IsAuthServer { get; set; }

}