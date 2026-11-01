namespace AuthServer.Web.Dto.Admin.Users;

public class AuthServerRoleRemoveRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}
