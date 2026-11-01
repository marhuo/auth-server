namespace AuthServer.Domain.Users;

public class UserPagedListQuery : PagedListQuery
{
    public Guid? TenantId { get; init; }
}
