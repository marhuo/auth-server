namespace AuthServer.Web.Dto.Api;

public class TenantCreateRequest
{
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required string AdminEmail { get; init; }
    public List<string> AdminRoleNames { get; init; } = [];
}
