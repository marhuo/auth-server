namespace AuthServer.Web.Dto.Admin.Users;

public class UserRoleDto
{
    public Guid RoleId { get; set; }
    public Guid? TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsAuthServerRole { get; set; }
}
