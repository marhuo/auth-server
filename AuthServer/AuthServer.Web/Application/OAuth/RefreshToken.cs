namespace AuthServer.Web.Application.OAuth;

public class RefreshToken
{
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public Guid FamilyId { get; init; }
    public List<string> Scopes { get; init; } = [];
}
