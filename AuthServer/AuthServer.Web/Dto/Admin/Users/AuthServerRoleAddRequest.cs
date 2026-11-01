namespace AuthServer.Web.Dto.Admin.Users;

public class AuthServerRoleAddRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}
