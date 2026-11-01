namespace AuthServer.Web.Dto.Admin.Scopes;

public class ScopeCreateRequest
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public Guid? ResourceServerId { get; set; }
}
