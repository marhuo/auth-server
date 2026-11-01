namespace AuthServer.Web.Dto.Admin.Users;

public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public List<UserRoleDto> UserRoles { get; set; } = [];
}
