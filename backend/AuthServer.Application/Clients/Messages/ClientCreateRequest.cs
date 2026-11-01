namespace AuthServer.Application.Clients.Messages;

public class ClientCreateRequest
{
    public required ClientDtoTypes Type { get; init; }
    public required string ClientId { get; init; }
    public required string DisplayName { get; init; }
    public string? Secret { get; init; }
    public required Guid TargetResourceServerId { get; init; }

    // Set when the client belongs to a specific tenant.
    public Guid? TenantId { get; init; }

    public List<string> RedirectUris { get; init; } = [];
    public List<string> PostLogoutRedirectUris { get; init; } = [];
    public List<Guid> ScopeIds { get; init; } = [];

    // See Client.InitiateLoginUri. Optional; only for clients that sign in users.
    public string? InitiateLoginUri { get; init; }
}
