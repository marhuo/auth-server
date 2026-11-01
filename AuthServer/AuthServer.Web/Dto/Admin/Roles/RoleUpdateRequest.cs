namespace AuthServer.Web.Dto.Admin.Roles;

public class RoleUpdateRequest
{
    public Guid Id { get; set; }
    public Guid ResourceServerId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}
