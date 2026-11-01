namespace AuthServer.Application.Users.Messages;

public class UserInviteRequest
{
    public required string Email { get; init; }

    // Roles to add. Each role is validated like UserService.AddRoleAsync: auth server roles have no
    // tenant, tenant roles need one.
    public List<UserRoleDto> UserRoles { get; init; } = [];
}
