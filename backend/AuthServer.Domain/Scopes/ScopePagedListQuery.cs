namespace AuthServer.Domain.Scopes;

public class ScopePagedListQuery : PagedListQuery
{
    public Guid? ResourceServerId { get; init; }
}
