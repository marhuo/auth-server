using AuthServer.Application.Common.Messages;

namespace AuthServer.Application.Scopes.Messages;

public class ScopePagedListRequest : PagedListRequest
{
    public Guid? ResourceServerId { get; init; }
}
