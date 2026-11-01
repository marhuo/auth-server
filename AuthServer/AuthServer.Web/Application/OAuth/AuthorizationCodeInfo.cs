namespace AuthServer.Web.Application.OAuth;

public class AuthorizationCodeInfo
{
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public List<string> Scopes { get; init; } = [];
    public string? Nonce { get; init; }
}