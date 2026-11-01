using AuthServer.Domain.Clients;

namespace AuthServer.Application.Clients.Messages;

public class ClientDto
{
    public required Guid Id { get; init; }
    public required ClientDtoTypes Type { get; init; }
    public required string ClientId { get; init; }
    public required string DisplayName { get; init; }
    public required bool IsActive { get; init; }
    public required Guid ResourceServerId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    // Set when the client belongs to a specific tenant.
    public required Guid? TenantId { get; init; }

    public required List<string> RedirectUris { get; init; }
    public required List<string> PostLogoutRedirectUris { get; init; }
    public required List<Guid> ScopeIds { get; init; }
    public required List<string> AllowedGrantTypes { get; init; }

    // Token lifetimes in seconds. Null when the client does not get that token.
    public required int AccessTokenLifetimeSeconds { get; init; }
    public required int? IdTokenLifetimeSeconds { get; init; }
    public required int? RefreshTokenLifetimeSeconds { get; init; }

    public static ClientDto ToDto(Client client) => new()
    {
        Id = client.Id,
        Type = client.Type.ToDto(),
        ClientId = client.ClientId,
        DisplayName = client.DisplayName,
        IsActive = client.IsActive,
        ResourceServerId = client.ResourceServerId,
        CreatedAt = client.CreatedAt,
        TenantId = client.TenantId,
        RedirectUris = [.. client.RedirectUris],
        PostLogoutRedirectUris = [.. client.PostLogoutRedirectUris],
        ScopeIds = [.. client.ScopeIds],
        AllowedGrantTypes = [.. client.GrantTypes],
        AccessTokenLifetimeSeconds = ToSeconds(client.AccessTokenLifetime),
        IdTokenLifetimeSeconds = client.IdTokenLifetime is { } idToken ? ToSeconds(idToken) : null,
        RefreshTokenLifetimeSeconds = client.RefreshTokenLifetime is { } refreshToken ? ToSeconds(refreshToken) : null
    };

    private static int ToSeconds(TimeSpan lifetime) => (int)lifetime.TotalSeconds;
}
