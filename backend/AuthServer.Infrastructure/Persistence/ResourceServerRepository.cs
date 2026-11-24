using AuthServer.Domain.Common;
using AuthServer.Domain.ResourceServers;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AuthServer.Infrastructure.Persistence;

public class ResourceServerRepository(AuthDbContext db) : IResourceServerRepository
{
    public async Task<ResourceServer?> GetByIdAsync(Guid id)
    {
        var entity = await db.ResourceServers.AsNoTracking().SingleOrDefaultAsync(rs => rs.Id == id);
        return entity?.ToDomain();
    }

    public async Task<ResourceServer> GetAuthServerAsync()
    {
        var entity = await db.ResourceServers.AsNoTracking().SingleAsync(rs => rs.IsAuthServer);
        return entity.ToDomain();
    }

    public async Task<List<ResourceServer>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0)
            return [];

        var distinctIds = ids.Distinct().ToList();

        var entities = await db.ResourceServers
            .AsNoTracking()
            .Where(rs => distinctIds.Contains(rs.Id))
            .OrderBy(rs => rs.DisplayName)
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<List<ResourceServer>> GetAllAsync()
    {
        var entities = await db.ResourceServers
            .AsNoTracking()
            .OrderBy(rs => rs.DisplayName)
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public Task<PagedList<ResourceServer>> GetPagedListAsync(PagedListQuery query)
    {
        var resourceServers = db.ResourceServers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var pattern = query.SearchTerm.ToContainsPattern();
            resourceServers = resourceServers.Where(rs => EF.Functions.ILike(rs.Audience, pattern));
        }

        return resourceServers.OrderBy(rs => rs.Audience).ToPagedListAsync(query, e => e.ToDomain());
    }

    public Task<bool> ExistsAsync(Guid id) =>
        db.ResourceServers.AnyAsync(rs => rs.Id == id);

    public Task<bool?> IsAuthServerAsync(Guid id) =>
        db.ResourceServers
            .Where(rs => rs.Id == id)
            .Select(rs => (bool?)rs.IsAuthServer)
            .SingleOrDefaultAsync();

    public async Task<ResourceServer?> GetByAudienceAsync(string audience)
    {
        var entity = await db.ResourceServers.AsNoTracking().SingleOrDefaultAsync(rs => rs.Audience == audience);
        return entity?.ToDomain();
    }

    public Task<bool> AudienceExistsAsync(string audience, Guid? excludeId = null) =>
        db.ResourceServers.AnyAsync(rs => rs.Audience == audience && rs.Id != excludeId);

    public async Task CreateAsync(ResourceServer resourceServer)
    {
        db.ResourceServers.Add(ResourceServerEntity.FromDomain(resourceServer));
        await SaveAsync();
    }

    public async Task UpdateAsync(ResourceServer resourceServer)
    {
        var entity = await db.ResourceServers.SingleAsync(rs => rs.Id == resourceServer.Id);
        entity.UpdateFrom(resourceServer);
        await SaveAsync();
    }

    public Task DeleteAsync(Guid id) =>
        db.ResourceServers.Where(rs => rs.Id == id).ExecuteDeleteAsync();

    private async Task SaveAsync()
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: AuthDbContext.ResourceServerAudienceIndex
        })
        {
            // A concurrent request took the audience after the validator checked it.
            db.ChangeTracker.Clear();
            throw new ValidationException(ResourceServer.AudienceTakenError());
        }
    }
}
