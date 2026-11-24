using AuthServer.Domain.Common;
using AuthServer.Domain.Scopes;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AuthServer.Infrastructure.Persistence;

public class ScopeRepository(AuthDbContext db) : IScopeRepository
{
    public async Task<Scope?> GetByIdAsync(Guid id)
    {
        var entity = await db.Scopes.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id);
        return entity?.ToDomain();
    }

    public async Task<List<Scope>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0)
            return [];

        var distinctIds = ids.Distinct().ToList();

        var entities = await db.Scopes
            .AsNoTracking()
            .Where(s => distinctIds.Contains(s.Id))
            .OrderBy(s => s.Name)
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public Task<PagedList<Scope>> GetPagedListAsync(ScopePagedListQuery query)
    {
        var scopes = db.Scopes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var pattern = query.SearchTerm.ToContainsPattern();
            scopes = scopes.Where(s =>
                EF.Functions.ILike(s.Name, pattern) ||
                EF.Functions.ILike(s.DisplayName, pattern));
        }

        if (query.ResourceServerId is { } resourceServerId)
            scopes = scopes.Where(s => s.ResourceServerId == resourceServerId);

        return scopes.OrderBy(s => s.Name).ToPagedListAsync(query, e => e.ToDomain());
    }

    public async Task<List<Scope>> GetByResourceServerIdAsync(Guid resourceServerId)
    {
        var entities = await db.Scopes
            .AsNoTracking()
            .Where(s => s.ResourceServerId == resourceServerId)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<List<Scope>> GetGlobalAsync()
    {
        var entities = await db.Scopes
            .AsNoTracking()
            .Where(s => s.ResourceServerId == null)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<List<Guid>> GetIdsByNamesAsync(IReadOnlyCollection<string> names)
    {
        if (names.Count == 0)
            return [];

        var distinctNames = names.Distinct().ToList();

        return await db.Scopes
            .Where(s => distinctNames.Contains(s.Name))
            .Select(s => s.Id)
            .ToListAsync();
    }

    public async Task<List<string>> GetAudiencesAsync(IReadOnlyCollection<string> names)
    {
        if (names.Count == 0)
            return [];

        var distinctNames = names.Distinct().ToList();

        return await db.Scopes
            .Where(s => distinctNames.Contains(s.Name) && s.ResourceServerId != null)
            .Select(s => s.ResourceServer!.Audience)
            .Distinct()
            .ToListAsync();
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null) =>
        db.Scopes.AnyAsync(s => s.Name == name && s.Id != excludeId);

    public Task<bool> IsAssignedToClientsAsync(Guid scopeId) =>
        db.Scopes.Where(s => s.Id == scopeId).SelectMany(s => s.Clients).AnyAsync();

    public Task<bool> AnyForResourceServerAsync(Guid resourceServerId) =>
        db.Scopes.AnyAsync(s => s.ResourceServerId == resourceServerId);

    public async Task CreateAsync(Scope scope)
    {
        db.Scopes.Add(ScopeEntity.FromDomain(scope));
        await SaveAsync();
    }

    public async Task UpdateAsync(Scope scope)
    {
        var entity = await db.Scopes.SingleAsync(s => s.Id == scope.Id);
        entity.UpdateFrom(scope);
        await SaveAsync();
    }

    public Task DeleteAsync(Guid id) =>
        db.Scopes.Where(s => s.Id == id).ExecuteDeleteAsync();

    private async Task SaveAsync()
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: AuthDbContext.ScopeNameIndex
        })
        {
            // A concurrent request took the name after the validator checked it.
            db.ChangeTracker.Clear();
            throw new ValidationException(Scope.NameTakenError());
        }
    }
}
