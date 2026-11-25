namespace AuthServer.Web.Controllers.Api.Dto;

public class TenantCreateRequest
{
    public required string Name { get; init; }
    public required string AdminEmail { get; init; }
    public List<string> AdminRoleNames { get; init; } = [];

    // Which app the user is invited to, e.g. "razor-pages-demo-web". The invitation email links to
    // that app's sign-in (Client.InitiateLoginUri). Optional.
    public string? ClientId { get; init; }
}
