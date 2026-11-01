using AuthServer.Web.Persistence;

namespace AuthServer.Web.Application.Clients;

public class Client
{
    public Guid Id { get; set; }
    public ClientTypes Type { get; set; }
    public string ClientId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public Guid ResourceServerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? TenantId { get; set; }
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<Guid> Scopes { get; set; } = [];

    public bool IsConfidential =>
        Type is ClientTypes.ConfidentialApplication or ClientTypes.ConfidentialMachine;

    public bool SignsInUsers =>
        Type is ClientTypes.PublicApplication or ClientTypes.ConfidentialApplication;

    public bool IsGrantTypeAllowed(string? grantType) =>
        !string.IsNullOrEmpty(grantType) && ClientGrants.For(Type).Contains(grantType);

    public bool IsRedirectUriAllowed(string? uri) =>
        !string.IsNullOrEmpty(uri) && RedirectUris.Contains(uri, StringComparer.Ordinal);

    public bool IsPostLogoutRedirectUriAllowed(string? uri) =>
        !string.IsNullOrEmpty(uri) && PostLogoutRedirectUris.Contains(uri, StringComparer.Ordinal);

    public bool AreScopesAllowed(IEnumerable<Guid> scopeIds) =>  scopeIds.All(Scopes.Contains);
}