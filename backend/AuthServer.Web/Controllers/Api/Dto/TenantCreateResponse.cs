namespace AuthServer.Web.Controllers.Api.Dto;

public class TenantCreateResponse
{
    public required Guid TenantId { get; init; }
    public required Guid UserId { get; init; }

    // Generated from the tenant name.
    public required string Slug { get; init; }
}
