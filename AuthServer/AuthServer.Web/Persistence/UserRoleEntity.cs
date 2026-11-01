namespace AuthServer.Web.Persistence;

public class UserRoleEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public virtual UserEntity User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public virtual RoleEntity Role { get; set; } = null!;
    public Guid? TenantId { get; set; }
    public virtual TenantEntity? Tenant { get; set; }
}

