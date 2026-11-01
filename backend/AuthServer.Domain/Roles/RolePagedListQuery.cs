namespace AuthServer.Domain.Roles;

public class RolePagedListQuery : PagedListQuery
{
    public Guid? ResourceServerId { get; init; }
}
