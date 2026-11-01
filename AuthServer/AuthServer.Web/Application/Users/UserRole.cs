namespace AuthServer.Web.Application.Users;

public class UserRole
{
    public required Guid RoleId { get; init; }
    public required Guid? TenantId { get; init; }
}
