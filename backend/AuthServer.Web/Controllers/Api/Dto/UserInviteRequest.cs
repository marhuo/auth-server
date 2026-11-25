namespace AuthServer.Web.Controllers.Api.Dto;

public class UserInviteRequest
{
    public required string Email { get; init; }
    public required Guid TenantId { get; init; }
    public List<string> RoleNames { get; init; } = [];

    // Which app the user is invited to, e.g. "razor-pages-demo-web". The invitation email links to
    // that app's sign-in (Client.InitiateLoginUri). Optional.
    public string? ClientId { get; init; }
}
