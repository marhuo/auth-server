using AuthServer.Domain.Common;
using AuthServer.Domain.Tenants;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AuthServer.Infrastructure.Persistence;

public class TenantRepository(AuthDbContext db) : ITenantRepository
{
    public async Task<Tenant?> GetByIdAsync(Guid id)
    {
        var entity = await db.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
        return entity?.ToDomain();
    }

    public async Task<List<Tenant>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0)
            return [];

        var distinctIds = ids.Distinct().ToList();

        var entities = await db.Tenants
            .AsNoTracking()
            .Where(t => distinctIds.Contains(t.Id))
            .OrderBy(t => t.NormalizedName)
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public Task<PagedList<Tenant>> GetPagedListAsync(PagedListQuery query)
    {
        var tenants = db.Tenants.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var pattern = Tenant.NormalizeName(query.SearchTerm).ToContainsPattern();
            tenants = tenants.Where(t => EF.Functions.Like(t.NormalizedName, pattern));
        }

        return tenants.OrderBy(t => t.NormalizedName).ToPagedListAsync(query, e => e.ToDomain());
    }

    public async Task<List<Tenant>> GetUserTenantsAsync(Guid userId, Guid resourceServerId)
    {
        var entities = await db.Tenants
            .AsNoTracking()
            .Where(t => t.SuspendedAt == null
                && t.UserRoles.Any(ur => ur.UserId == userId && ur.Role.ResourceServerId == resourceServerId))
            .OrderBy(t => t.NormalizedName)
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public Task<bool> ExistsAsync(Guid id) =>
        db.Tenants.AnyAsync(t => t.Id == id);

    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null) =>
        db.Tenants.AnyAsync(t => t.Slug == slug && t.Id != excludeId);

    public async Task CreateAsync(Tenant tenant)
    {
        db.Tenants.Add(TenantEntity.FromDomain(tenant));
        await SaveAsync();
    }

    public async Task UpdateAsync(Tenant tenant)
    {
        var entity = await db.Tenants.SingleAsync(t => t.Id == tenant.Id);
        entity.UpdateFrom(tenant);
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: AuthDbContext.TenantSlugIndex
        })
        {
            // A concurrent request took the slug after the validator checked it.
            db.ChangeTracker.Clear();
            throw new ValidationException(Tenant.SlugTakenError());
        }
    }
}
