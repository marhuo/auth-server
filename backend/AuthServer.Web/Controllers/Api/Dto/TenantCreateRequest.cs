namespace AuthServer.Web.Controllers.Api.Dto;

public class TenantCreateRequest
{
    public required string Name { get; init; }
    public required string AdminEmail { get; init; }
    public List<string> AdminRoleNames { get; init; } = [];
}
