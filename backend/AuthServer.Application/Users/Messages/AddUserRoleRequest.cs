namespace AuthServer.Application.Users.Messages;

public class AddUserRoleRequest
{
    public required Guid UserId { get; init; }
    public required Guid RoleId { get; init; }

    // Null for auth server roles.
    public Guid? TenantId { get; init; }
}
