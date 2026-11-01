namespace AuthServer.Web.Dto.Admin.Roles
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public Guid ResourceServerId { get; set; }
    }
}
