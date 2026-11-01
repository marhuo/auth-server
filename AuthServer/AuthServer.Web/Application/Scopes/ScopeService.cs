using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Web.Application.Scopes;

public class ScopeService(AuthDbContext db, ScopeValidator validator)
{
    public async Task<Guid> CreateAsync(string name, string displayName, Guid? resourceServerId)
    {
        name = name?.Trim() ?? "";
        displayName = displayName?.Trim() ?? "";

        await validator.ValidateCreateAsync(name, displayName, resourceServerId);

        var scopeEntity = new ScopeEntity
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            DisplayName = displayName,
            ResourceServerId = resourceServerId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Scopes.Add(scopeEntity);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            if (await db.Scopes.AnyAsync(s => s.Name == name))
                throw ScopeValidator.NameTaken();

            throw;
        }

        return scopeEntity.Id;
    }

    public async Task UpdateAsync(Guid id, string name, string displayName, Guid? resourceServerId)
    {
        name = name?.Trim() ?? "";
        displayName = displayName?.Trim() ?? "";

        var scopeEntity = await FindWithResourceServerAsync(id);

        await validator.ValidateUpdateAsync(scopeEntity, name, displayName, resourceServerId);

        scopeEntity.Name = name;
        scopeEntity.DisplayName = displayName;
        scopeEntity.ResourceServerId = resourceServerId;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            if (await db.Scopes.AnyAsync(s => s.Name == name && s.Id != id))
                throw ScopeValidator.NameTaken();

            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var scope = await FindWithResourceServerAsync(id);

        await validator.ValidateDeleteAsync(scope);

        await db.Scopes.Where(s => s.Id == id).ExecuteDeleteAsync();
    }

    public Task<Scope?> GetByIdAsync(Guid id) =>
        db.Scopes
            .Where(s => s.Id == id)
            .Select(ScopeMapper.ToDomainExpression)
            .SingleOrDefaultAsync();

    public async Task<List<Scope>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
            return [];

        var distinctIds = ids.Distinct().ToList();

        return await db.Scopes
            .Where(s => distinctIds.Contains(s.Id))
            .OrderBy(s => s.Name)
            .Select(ScopeMapper.ToDomainExpression)
            .ToListAsync();
    }

    public Task<PagedList<Scope>> GetPagedListAsync(string? searchTerm, Guid? resourceServerId, int pageNumber, int pageSize)
    {
        var scopes = db.Scopes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{EscapeLike(searchTerm.Trim())}%";
            scopes = scopes.Where(s =>
                EF.Functions.ILike(s.Name, pattern) ||
                EF.Functions.ILike(s.DisplayName, pattern));
        }

        if (resourceServerId is { } id)
        {
            scopes = scopes.Where(s => s.ResourceServerId == id);
        }

        return Task.FromResult(scopes
            .OrderBy(s => s.Name)
            .Select(ScopeMapper.ToDomainExpression)
            .ToPagedList(pageNumber, pageSize));
    }

    public Task<List<Scope>> GetByResourceServerIdAsync(Guid resourceServerId) =>
        db.Scopes
            .Where(s => s.ResourceServerId == resourceServerId)
            .OrderBy(s => s.Name)
            .Select(ScopeMapper.ToDomainExpression)
            .ToListAsync();

    public Task<List<Scope>> GetGlobalAsync() =>
        db.Scopes
            .Where(s => s.ResourceServerId == null)
            .OrderBy(s => s.Name)
            .Select(ScopeMapper.ToDomainExpression)
            .ToListAsync();

    public Task<Guid?> GetIdByNameAsync(string name) =>
        db.Scopes
            .Where(s => s.Name == name)
            .Select(s => (Guid?)s.Id)
            .SingleOrDefaultAsync();

    public async Task<List<Guid>> GetIdsByNamesAsync(IEnumerable<string> names)
    {
        var list = (names ?? []).Distinct().ToList();

        if (list.Count == 0)
            return [];

        return await db.Scopes
            .Where(s => list.Contains(s.Name))
            .Select(s => s.Id)
            .ToListAsync();
    }

    public async Task<List<string>> GetAudiencesAsync(IEnumerable<string> scopeNames)
    {
        var list = (scopeNames ?? []).Distinct().ToList();

        if (list.Count == 0)
            return [];

        return await db.Scopes
            .Where(s => list.Contains(s.Name) && s.ResourceServerId != null)
            .Select(s => s.ResourceServer!.Audience)
            .Distinct()
            .ToListAsync();
    }

    private async Task<ScopeEntity> FindWithResourceServerAsync(Guid id) =>
        await db.Scopes
            .Include(s => s.ResourceServer)
            .SingleOrDefaultAsync(s => s.Id == id)
        ?? throw new ValidationException(Errors.NotFound(nameof(id), "Scope", "Scopea"));

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}