namespace AuthServer.Web.Dto.Admin.Users;

public class AddUserRoleRequest
{
    public required Guid UserId { get; init; }
    public required Guid RoleId { get; init; }
    public Guid? TenantId { get; init; }
}
