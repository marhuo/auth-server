namespace AuthServer.Web.Persistence;

public class RoleEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public virtual List<UserRoleEntity> UserRoles { get; set; } = [];
    public Guid ResourceServerId { get; set; }
    public virtual ResourceServerEntity ResourceServer { get; set; } = null!;
}

