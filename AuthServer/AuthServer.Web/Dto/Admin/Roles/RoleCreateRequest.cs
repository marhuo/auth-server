namespace AuthServer.Web.Dto.Admin.Roles;

public class RoleCreateRequest
{
    public Guid ResourceServerId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}
