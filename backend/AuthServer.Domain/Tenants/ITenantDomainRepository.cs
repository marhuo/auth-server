namespace AuthServer.Domain.Tenants;

public interface ITenantDomainRepository
{
    Task<TenantDomain?> GetByIdAsync(Guid id);

    // Ordered by name.
    Task<List<TenantDomain>> GetByTenantIdAsync(Guid tenantId);

    // Verified domains of all tenants, ordered by id: the next batch after afterId.
    Task<List<TenantDomain>> GetVerifiedBatchAsync(Guid? afterId, int batchSize);

    Task<bool> NameExistsInTenantAsync(Guid tenantId, string name);

    Task<bool> VerifiedByOtherTenantAsync(string name, Guid tenantId);

    Task CreateAsync(TenantDomain domain);

    Task UpdateAsync(TenantDomain domain);

    Task DeleteAsync(Guid id);
}
