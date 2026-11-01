namespace AuthServer.Domain.Clients;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id);

    Task<Client?> GetByClientIdAsync(string clientId);

    // Active (not deactivated) client.
    Task<Client?> FindActiveByClientIdAsync(string clientId);

    // Ordered by client id.
    Task<List<Client>> GetByIdsAsync(IReadOnlyCollection<Guid> ids);

    // Ordered by client id. SearchTerm matches the client id and the display name.
    Task<PagedList<Client>> GetPagedListAsync(PagedListQuery query);

    // Whether any active client has registered the URI as a post-logout redirect URI.
    Task<bool> IsPostLogoutRedirectUriRegisteredAsync(string uri);

    Task<bool> ClientIdExistsAsync(string clientId, Guid? excludeId = null);

    Task<bool> AnyForTargetResourceServerAsync(Guid resourceServerId);

    Task<bool> AnyForTenantAsync(Guid tenantId);

    Task CreateAsync(Client client);

    // When the client becomes deleted in this update, its refresh tokens and authorization
    // codes are removed in the same transaction.
    Task UpdateAsync(Client client);
}
