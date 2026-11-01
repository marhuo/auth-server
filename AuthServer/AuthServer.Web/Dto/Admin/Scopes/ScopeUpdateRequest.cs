namespace AuthServer.Web.Dto.Admin.Scopes;

public class ScopeUpdateRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public Guid? ResourceServerId { get; set; }
}
