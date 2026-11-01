using AuthServer.Application.Common.Messages;

namespace AuthServer.Application.Users.Messages;

public class UserPagedListRequest : PagedListRequest
{
    public required Guid? TenantId { get; init; }
}
