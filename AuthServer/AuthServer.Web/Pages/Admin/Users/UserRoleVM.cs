namespace AuthServer.Web.Pages.Admin
{
    public class UserRoleVM
    {
        public required Guid RoleId { get;  init; }
        public required string RoleName { get; init; }
        public required Guid? TenantId { get; init; }
        public required string TenantName { get; init; }
    }
}
