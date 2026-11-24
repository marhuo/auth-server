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

    // Physically deletes the tenant with its user roles and domains.
    Task DeleteAsync(Guid id);

    // Physically deletes tenants that nothing points to (no user roles, clients or domains) and that
    // were created before the given time. Returns the number of deleted tenants.
    Task<int> DeleteUnusedAsync(DateTimeOffset createdBefore);
}
