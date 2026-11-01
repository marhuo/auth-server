namespace AuthServer.Web.Dto.Api;

public class UserInviteRequest
{
    public required string Email { get; init; }
    public required Guid TenantId { get; init; }
    public List<string> RoleNames { get; init; } = [];
}
