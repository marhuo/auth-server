namespace AuthServer.Web.Dto.Admin.Users;

public class TenantRoleAddRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid TenantId { get; set; }
}
