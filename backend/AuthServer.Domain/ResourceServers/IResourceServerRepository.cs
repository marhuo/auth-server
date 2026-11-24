namespace AuthServer.Domain.ResourceServers;

public interface IResourceServerRepository
{
    Task<ResourceServer?> GetByIdAsync(Guid id);

    // The auth server's own resource server. Always exists (seeded at startup).
    Task<ResourceServer> GetAuthServerAsync();

    // Ordered by display name.
    Task<List<ResourceServer>> GetByIdsAsync(IReadOnlyCollection<Guid> ids);

    // Ordered by display name. The number of resource servers is small.
    Task<List<ResourceServer>> GetAllAsync();

    // Ordered by audience. SearchTerm matches the audience.
    Task<PagedList<ResourceServer>> GetPagedListAsync(PagedListQuery query);

    Task<bool> ExistsAsync(Guid id);

    Task<ResourceServer?> GetByAudienceAsync(string audience);

    // Returns null if the resource server does not exist.
    Task<bool?> IsAuthServerAsync(Guid id);

    Task<bool> AudienceExistsAsync(string audience, Guid? excludeId = null);

    Task CreateAsync(ResourceServer resourceServer);

    Task UpdateAsync(ResourceServer resourceServer);

    Task DeleteAsync(Guid id);
}
