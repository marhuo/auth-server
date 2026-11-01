namespace AuthServer.Web.Dto.Api;

public class TenantCreateResponse
{
    public required Guid TenantId { get; init; }
    public required Guid UserId { get; init; }
}
