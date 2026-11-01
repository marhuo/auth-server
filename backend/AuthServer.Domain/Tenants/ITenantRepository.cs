namespace AuthServer.Domain.Tenants;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id);

    // Ordered by normalized name.
    Task<List<Tenant>> GetByIdsAsync(IReadOnlyCollection<Guid> ids);

    // Ordered by normalized name. SearchTerm matches the name (case-insensitive).
    Task<PagedList<Tenant>> GetPagedListAsync(PagedListQuery query);

    // Tenants that are not suspended and in which the user has a role of the resource server.
    // Ordered by normalized name.
    Task<List<Tenant>> GetUserTenantsAsync(Guid userId, Guid resourceServerId);

    Task<bool> ExistsAsync(Guid id);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null);

    Task CreateAsync(Tenant tenant);

    Task UpdateAsync(Tenant tenant);
}
