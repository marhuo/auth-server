using AuthServer.Domain.Clients;

namespace AuthServer.Infrastructure.Persistence.Ef;

public class ClientEntity : ISoftDeletableEntity
{
    public Guid Id { get; set; }
    public ClientTypes Type { get; set; }
    public string ClientId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? SecretHash { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid ResourceServerId { get; set; }
    public virtual ResourceServerEntity ResourceServer { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? TenantId { get; set; } // If the client is associated with a specific tenant, this field will be set. Otherwise, it will be null.
    public virtual TenantEntity? Tenant { get; set; }
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<ScopeEntity> Scopes { get; set; } = [];
    public TimeSpan AccessTokenLifetime { get; set; }
    public TimeSpan? IdTokenLifetime { get; set; }
    public TimeSpan? RefreshTokenLifetime { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // Scopes are not set here; the repository syncs them because it needs the scope entities.
    public static ClientEntity FromDomain(Client client)
    {
        var entity = new ClientEntity
        {
            Id = client.Id,
            Type = client.Type,
            ResourceServerId = client.ResourceServerId,
            CreatedAt = client.CreatedAt
        };
        entity.UpdateFrom(client);
        return entity;
    }

    // Scopes are not set here; the repository syncs them because it needs the scope entities.
    public void UpdateFrom(Client client)
    {
        ClientId = client.ClientId;
        DisplayName = client.DisplayName;
        SecretHash = client.SecretHash;
        IsActive = client.IsActive;
        TenantId = client.TenantId;
        RedirectUris = [.. client.RedirectUris];
        PostLogoutRedirectUris = [.. client.PostLogoutRedirectUris];
        AccessTokenLifetime = client.AccessTokenLifetime;
        IdTokenLifetime = client.IdTokenLifetime;
        RefreshTokenLifetime = client.RefreshTokenLifetime;
        DeletedAt = client.DeletedAt;
    }

    // Scopes must be loaded.
    public Client ToDomain() => new(
        id: Id,
        type: Type,
        clientId: ClientId,
        displayName: DisplayName,
        secretHash: SecretHash,
        isActive: IsActive,
        resourceServerId: ResourceServerId,
        tenantId: TenantId,
        redirectUris: RedirectUris,
        postLogoutRedirectUris: PostLogoutRedirectUris,
        scopeIds: Scopes.Select(s => s.Id).ToList(),
        accessTokenLifetime: AccessTokenLifetime,
        idTokenLifetime: IdTokenLifetime,
        refreshTokenLifetime: RefreshTokenLifetime,
        createdAt: CreatedAt,
        deletedAt: DeletedAt);
}
