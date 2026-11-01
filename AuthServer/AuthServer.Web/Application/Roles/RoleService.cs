using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Web.Application.Roles;

public class RoleService(AuthDbContext db, RoleValidator validator)
{
    public const int MaxPageSize = 100;

    public static string NormalizeRoleName(string name) => name.Trim().ToLowerInvariant();

    public async Task<Guid?> GetIdByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalized = NormalizeRoleName(name);

        return await db.Roles
            .Where(r => r.Name == normalized)
            .Select(r => (Guid?)r.Id)
            .SingleOrDefaultAsync();
    }

    public async Task<Dictionary<string, Guid>> GetIdsByNamesAsync(
    Guid resourceServerId,
    IEnumerable<string> names)
    {
        var normalized = names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(NormalizeRoleName)
            .Distinct()
            .ToList();

        if (normalized.Count == 0)
            return [];

        return await db.Roles
                    .Where(r => r.ResourceServerId == resourceServerId && normalized.Contains(r.Name))
            .ToDictionaryAsync(r => r.Name, r => r.Id);
    }

    public async Task<Guid> CreateAsync(string name, string description, Guid resourceServerId)
    {
        name = name?.Trim() ?? "";
        description = NormalizeDescription(description);

        await validator.ValidateCreateAsync(name, description, resourceServerId);

        var role = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            ResourceServerId = resourceServerId,
            Name = name,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Roles.Add(role);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            if (await db.Roles.AnyAsync(r => r.Name == name))
                throw RoleValidator.NameTaken();

            throw;
        }

        return role.Id;
    }

    public async Task UpdateAsync(Guid id, string name, string description, Guid resourceServerId)
    {
        name = name?.Trim() ?? "";
        description = NormalizeDescription(description);

        var role = await FindWithResourceServerAsync(id);

        await validator.ValidateUpdateAsync(role, name, description, resourceServerId);

        role.Name = name;
        role.Description = description;
        role.ResourceServerId = resourceServerId;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            if (await db.Roles.AnyAsync(r => r.Name == name && r.Id != id))
                throw RoleValidator.NameTaken();

            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await FindWithResourceServerAsync(id);

        await validator.ValidateDeleteAsync(role);

        await db.Roles.Where(r => r.Id == id).ExecuteDeleteAsync();
    }

    public Task<Role?> GetByIdAsync(Guid id) =>
        db.Roles
            .Where(r => r.Id == id)
            .Select(RoleMapper.ToDomainExpression)
            .SingleOrDefaultAsync();

    public async Task<List<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
            return [];

        var distinctIds = ids.Distinct().ToList();

        return await db.Roles
            .Where(r => distinctIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .Select(RoleMapper.ToDomainExpression)
            .ToListAsync();
    }

    public async Task<PagedList<Role>> GetPagedListAsync(string? searchTerm, Guid? resourceServerId, int pageNumber, int pageSize)
    {
        var roles = db.Roles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{EscapeLike(searchTerm.Trim())}%";
            roles = roles.Where(r =>
                EF.Functions.ILike(r.Name, pattern) ||
                EF.Functions.ILike(r.Description, pattern));
        }

        if (resourceServerId is { } id)
        {
            roles = roles.Where(r => r.ResourceServerId == id);
        }

        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = roles.OrderBy(r => r.Name).Select(RoleMapper.ToDomainExpression);

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        pageNumber = Math.Clamp(pageNumber, 1, totalPages);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedList<Role>(items, pageNumber, pageSize, totalCount);
    }

    private async Task<RoleEntity> FindWithResourceServerAsync(Guid id) =>
        await db.Roles
            .Include(r => r.ResourceServer)
            .SingleOrDefaultAsync(r => r.Id == id)
        ?? throw new ValidationException(Errors.NotFound(nameof(id), "Role", "Roolia"));

    private static string NormalizeDescription(string? description) => description?.Trim() ?? string.Empty;

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}