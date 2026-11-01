namespace AuthServer.Web.Dto.Admin.Clients;

public class ClientCreateRequest
{
    public ClientDtoTypes Type { get; set; }
    public string ClientId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Secret { get; set; }
    public Guid ResourceServerId { get; set; }
    public Guid? TenantId { get; set; } // If the client is associated with a specific tenant, this field will be set. Otherwise, it will be null.
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<Guid> ScopeIds { get; set; } = [];
}
