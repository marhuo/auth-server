using AuthServer.Web.Dto.Shared;

namespace AuthServer.Web.Dto.Admin.Scopes;

public class ScopePagedListRequest : PagedListRequest
{
    public Guid? ResourceServerId { get; set; }
}
