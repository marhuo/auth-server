using AuthServer.Web.Dto.Shared;

namespace AuthServer.Web.Dto.Admin.Roles;

public class RolePagedListRequest : PagedListRequest
{
    public Guid? ResourceServerId { get; set; }
}
