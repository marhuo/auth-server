using AuthServer.Application.Common.Messages;

namespace AuthServer.Application.Roles.Messages;

public class RolePagedListRequest : PagedListRequest
{
    public Guid? ResourceServerId { get; init; }
}
