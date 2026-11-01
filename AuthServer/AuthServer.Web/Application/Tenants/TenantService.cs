using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AuthServer.Web.Application.Tenants;

public class TenantService(AuthDbContext db, TenantValidator validator)
{
    public const int MaxPageSize = 100;

    public static string NormalizeName(string name) =>
        name.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();

    private static string NormalizeSlug(string? slug) => slug?.Trim().ToLowerInvariant() ?? "";

    public async Task<Guid> CreateAsync(string name)
    {
        name = name?.Trim() ?? "";
        var slug = Slug.From(name);

        await validator.ValidateCreateAsync(name, slug);

        var normalizedName = NormalizeName(name);

        var tenant = new TenantEntity
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            NormalizedName = normalizedName,
            Slug = slug,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Tenants.Add(tenant);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            await validator.ThrowIfTakenAsync(normalizedName, slug);

            throw;
        }

        return tenant.Id;
    }

    public async Task UpdateAsync(Guid id, string name, string slug)
    {
        name = name?.Trim() ?? "";
        slug = NormalizeSlug(slug);

        var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Id == id)
            ?? throw TenantNotFound(nameof(id));

        await validator.ValidateUpdateAsync(tenant.Id, name, slug);

        var normalizedName = NormalizeName(name);

        tenant.Name = name;
        tenant.NormalizedName = normalizedName;
        tenant.Slug = slug;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            await validator.ThrowIfTakenAsync(normalizedName, slug, id);

            throw;
        }
    }

    public async Task SuspendAsync(Guid id, string suspensionReason)
    {
        var reason = suspensionReason?.Trim() ?? "";

        validator.ValidateSuspend(reason);

        var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Id == id)
            ?? throw TenantNotFound(nameof(id));

        tenant.SuspendedAt ??= DateTimeOffset.UtcNow;
        tenant.SuspensionReason = reason;
        await db.SaveChangesAsync();
    }

    public async Task UnsuspendAsync(Guid id)
    {
        var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Id == id)
            ?? throw TenantNotFound(nameof(id));

        tenant.SuspendedAt = null;
        tenant.SuspensionReason = null;
        await db.SaveChangesAsync();
    }

    public Task<Tenant?> GetByIdAsync(Guid id) =>
        db.Tenants
            .Where(t => t.Id == id)
            .Select(TenantMapper.ToDomainExpression)
            .SingleOrDefaultAsync();

    public async Task<List<Tenant>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
            return [];

        var distinctIds = ids.Distinct().ToList();

        return await db.Tenants
            .Where(t => distinctIds.Contains(t.Id))
            .OrderBy(t => t.NormalizedName)
            .Select(TenantMapper.ToDomainExpression)
            .ToListAsync();
    }

    public async Task<PagedList<Tenant>> GetPagedListAsync(string? searchTerm, int pageNumber, int pageSize)
    {
        var tenants = db.Tenants.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = NormalizeName(searchTerm);
            tenants = tenants.Where(t => t.NormalizedName.Contains(term));
        }

        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = tenants.OrderBy(t => t.NormalizedName).Select(TenantMapper.ToDomainExpression);

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        pageNumber = Math.Clamp(pageNumber, 1, totalPages);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedList<Tenant>(items, pageNumber, pageSize, totalCount);
    }

    public async Task<Guid?> GetIdByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalizedName = NormalizeName(name);

        return await db.Tenants
            .Where(t => t.NormalizedName == normalizedName)
            .Select(t => (Guid?)t.Id)
            .SingleOrDefaultAsync();
    }

    public Task<List<Guid>> GetUserTenantIdsAsync(Guid userId, Guid resourceServerId, DateTimeOffset currentTime) =>
        UserTenantsQuery(userId, resourceServerId, currentTime)
            .Select(t => t.Id)
            .ToListAsync();

    public Task<List<Tenant>> GetUserTenantsAsync(Guid userId, Guid resourceServerId, DateTimeOffset currentTime) =>
        UserTenantsQuery(userId, resourceServerId, currentTime)
            .OrderBy(t => t.NormalizedName)
            .Select(TenantMapper.ToDomainExpression)
            .ToListAsync();

    private IQueryable<TenantEntity> UserTenantsQuery(Guid userId, Guid resourceServerId, DateTimeOffset currentTime) =>
        db.Tenants
            .AsNoTracking()
            .Where(t => t.SuspendedAt == null
                && t.UserRoles.Any(ur => ur.UserId == userId && ur.Role.ResourceServerId == resourceServerId));

    private static ValidationException TenantNotFound(string key) =>
        new(Errors.NotFound(key, "Tenant", "Asiakasta"));

    public async Task DeleteAsync(Guid id)
    {
        if (!await db.Tenants.AnyAsync(t => t.Id == id))
            throw TenantNotFound(nameof(id));

        await validator.ValidateDeleteAsync(id);

        await db.Tenants.Where(t => t.Id == id).ExecuteDeleteAsync();
    }
}