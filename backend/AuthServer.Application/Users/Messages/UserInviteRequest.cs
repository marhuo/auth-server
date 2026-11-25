namespace AuthServer.Application.Users.Messages;

public class UserInviteRequest
{
    public required string Email { get; init; }

    // Roles to add. Each role is validated like UserService.AddRoleAsync: auth server roles have no
    // tenant, tenant roles need one.
    public List<UserRoleDto> UserRoles { get; init; } = [];

    // Which app the user is invited to, e.g. "razor-pages-demo-web". The invitation email links to
    // that app's sign-in (Client.InitiateLoginUri). Optional.
    public string? ClientId { get; init; }
}
