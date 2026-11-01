using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Web.Application.ResourceServers;

public class ResourceServerService(AuthDbContext db, ResourceServerValidator validator)
{
    public async Task<Guid> CreateAsync(string audience, string displayName)
    {
        audience = audience?.Trim() ?? "";
        displayName = displayName?.Trim() ?? "";

        await validator.ValidateCreateAsync(audience, displayName);

        var entity = new ResourceServerEntity
        {
            Id = Guid.CreateVersion7(),
            Audience = audience,
            DisplayName = displayName,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ResourceServers.Add(entity);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            if (await db.ResourceServers.AnyAsync(rs => rs.Audience == audience))
                throw ResourceServerValidator.AudienceTaken();

            throw;
        }

        return entity.Id;
    }

    public async Task UpdateAsync(Guid id, string audience, string displayName)
    {
        audience = audience?.Trim() ?? "";
        displayName = displayName?.Trim() ?? "";

        var entity = await db.ResourceServers.SingleOrDefaultAsync(rs => rs.Id == id)
            ?? throw ResourceServerNotFound(nameof(id));

        await validator.ValidateUpdateAsync(entity, audience, displayName);

        entity.Audience = audience;
        entity.DisplayName = displayName;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            if (await db.ResourceServers.AnyAsync(rs => rs.Audience == audience && rs.Id != id))
                throw ResourceServerValidator.AudienceTaken();

            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await db.ResourceServers.AsNoTracking().SingleOrDefaultAsync(rs => rs.Id == id)
            ?? throw ResourceServerNotFound(nameof(id));

        await validator.ValidateDeleteAsync(entity);

        await db.ResourceServers.Where(rs => rs.Id == id).ExecuteDeleteAsync();
    }

    public async Task<Guid?> GetIdByAudienceAsync(string audience)
    {
        if (string.IsNullOrWhiteSpace(audience))
            return null;

        var value = audience.Trim().ToLowerInvariant();

        return await db.ResourceServers
            .Where(rs => rs.Audience == value)
            .Select(rs => (Guid?)rs.Id)
            .SingleOrDefaultAsync();
    }

    public Task<ResourceServer?> GetByIdAsync(Guid id) =>
        db.ResourceServers
            .Where(rs => rs.Id == id)
            .Select(ResourceServerMapper.ToDomainExpression)
            .SingleOrDefaultAsync();

    public Task<PagedList<ResourceServer>> GetPagedListAsync(string? searchTerm, int pageNumber, int pageSize)
    {
        var resourceServers = db.ResourceServers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            resourceServers = resourceServers.Where(rs => rs.Audience.Contains(term));
        }

        var page = resourceServers
            .OrderBy(rs => rs.Audience)
            .Select(ResourceServerMapper.ToDomainExpression)
            .ToPagedList(pageNumber, pageSize);

        return Task.FromResult(page);
    }

    public async Task<List<ResourceServer>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = (ids ?? []).Distinct().ToList();

        if (idList.Count == 0)
            return [];

        return await db.ResourceServers
            .Where(rs => idList.Contains(rs.Id))
            .OrderBy(rs => rs.DisplayName)
            .Select(ResourceServerMapper.ToDomainExpression)
            .ToListAsync();
    }

    public async Task<List<ResourceServer>> GetAllAsync() =>
        await db.ResourceServers
            .OrderBy(rs => rs.DisplayName)
            .Select(ResourceServerMapper.ToDomainExpression)
            .ToListAsync();

    private static ValidationException ResourceServerNotFound(string key) =>
        new(Errors.NotFound(key, "Resource server", "Resurssipalvelinta"));
}