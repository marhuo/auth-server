namespace AuthServer.Application.Users.Messages;

public class UserInviteRequest
{
    public required string Email { get; init; }

    // Roles to add. Each role is validated like UserService.AddRoleAsync: auth server roles have no
    // tenant, tenant roles need one.
    public List<UserRoleDto> UserRoles { get; init; } = [];

    // Which app the user is invited to, e.g. "razor-pages-demo-web". After setting the password, the user
    // gets a button to sign in to that app. Optional.
    public string? ClientId { get; init; }
}
