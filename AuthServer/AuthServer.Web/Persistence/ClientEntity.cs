using AuthServer.Web.Application.Clients;

namespace AuthServer.Web.Persistence;

public class ClientEntity
{
    public Guid Id { get; set; }
    public ClientTypes Type { get; set; }
    public string ClientId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? SecretHash { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid ResourceServerId { get; set; }
    public virtual ResourceServerEntity ResourceServer { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? TenantId { get; set; } // If the client is associated with a specific tenant, this field will be set. Otherwise, it will be null.
    public virtual TenantEntity? Tenant { get; set; }
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<ScopeEntity> Scopes { get; set; } = [];
    public List<string> AllowedGrantTypes { get; set; } = [];
}


