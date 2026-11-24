using AuthServer.Domain.Common;
using AuthServer.Domain.Tenants;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AuthServer.Infrastructure.Persistence;

public class TenantDomainRepository(AuthDbContext db) : ITenantDomainRepository
{
    public async Task<TenantDomain?> GetByIdAsync(Guid id)
    {
        var entity = await db.TenantDomains.AsNoTracking().SingleOrDefaultAsync(d => d.Id == id);
        return entity?.ToDomain();
    }

    public async Task<List<TenantDomain>> GetByTenantIdAsync(Guid tenantId)
    {
        var entities = await db.TenantDomains
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .OrderBy(d => d.Name)
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<List<TenantDomain>> GetVerifiedBatchAsync(Guid? afterId, int batchSize)
    {
        var entities = await db.TenantDomains
            .AsNoTracking()
            .Where(d => d.VerifiedAt != null && (afterId == null || d.Id > afterId))
            .OrderBy(d => d.Id)
            .Take(batchSize)
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public Task<bool> NameExistsInTenantAsync(Guid tenantId, string name) =>
        db.TenantDomains.AnyAsync(d => d.TenantId == tenantId && d.Name == name);

    public Task<bool> VerifiedByOtherTenantAsync(string name, Guid tenantId) =>
        db.TenantDomains.AnyAsync(d => d.Name == name && d.VerifiedAt != null && d.TenantId != tenantId);

    public async Task CreateAsync(TenantDomain domain)
    {
        db.TenantDomains.Add(TenantDomainEntity.FromDomain(domain));
        await db.SaveChangesAsync();
    }

    public Task DeleteAsync(Guid id) =>
        db.TenantDomains.Where(d => d.Id == id).ExecuteDeleteAsync();

    public async Task UpdateAsync(TenantDomain domain)
    {
        var entity = await db.TenantDomains.SingleAsync(d => d.Id == domain.Id);
        entity.UpdateFrom(domain);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: AuthDbContext.TenantDomainVerifiedNameIndex
        })
        {
            // Another tenant verified the domain after the validator checked it.
            db.ChangeTracker.Clear();
            throw new ValidationException(TenantDomain.VerifiedByOtherTenantError());
        }
    }
}
