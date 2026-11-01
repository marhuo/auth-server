namespace AuthServer.Web.Controllers.Api.Dto;

public class TenantCreateRequest
{
    public required string Name { get; init; }
    public required string AdminEmail { get; init; }
    public List<string> AdminRoleNames { get; init; } = [];

    // Which app the user is invited to, e.g. "muistio-web". After setting the password, the user
    // gets a button to sign in to that app. Optional.
    public string? ClientId { get; init; }
}
