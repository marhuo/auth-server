using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace AuthServer.Web.Application.ResourceServers;

public partial class ResourceServerService(AuthDbContext db)
{
    public const int MaxDisplayNameLength = 200;
    public const int MinDisplayNameLength = 4;

    [GeneratedRegex(@"^[a-z0-9][a-z0-9._-]{3,199}\z")]
    private static partial Regex AudienceRegex();

    private static readonly Expression<Func<ResourceServerEntity, ResourceServer>> ToResourceServerExpression =
        e => new ResourceServer
        {
            Id = e.Id,
            Audience = e.Audience,
            DisplayName = e.DisplayName
        };

    public async Task<Guid> CreateAsync(string audience, string displayName)
    {
        audience = audience?.Trim() ?? "";
        displayName = displayName?.Trim() ?? "";

        var errors = new ErrorCollector();

        if (!AudienceRegex().IsMatch(audience))
            errors.Add("audience", ErrorCode.InvalidFormat,
                "Audience may contain only lowercase letters, digits, '.', '_' and '-' (4-200 characters).",
                "Tunniste (Audience) saa sisältää vain pieniä kirjaimia, numeroita sekä merkit '.', '_' ja '-' (4–200 merkkiä).");

        ValidateDisplayName(errors, displayName);
        errors.ThrowIfAny();

        if (await db.ResourceServers.AnyAsync(rs => rs.Audience == audience))
            throw AudienceTaken();

        var entity = new ResourceServerEntity
        {
            Id = Guid.CreateVersion7(),
            Audience = audience,
            DisplayName = displayName
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
                throw AudienceTaken();

            throw;
        }

        return entity.Id;
    }

    public async Task UpdateAsync(Guid id, string displayName)
    {
        displayName = displayName?.Trim() ?? "";

        var errors = new ErrorCollector();
        ValidateDisplayName(errors, displayName);
        errors.ThrowIfAny();

        var entity = await db.ResourceServers.SingleOrDefaultAsync(rs => rs.Id == id)
            ?? throw new ValidationException(Errors.NotFound(nameof(id), "Resource server", "Resurssipalvelinta"));

        entity.DisplayName = displayName;
        await db.SaveChangesAsync();
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
            .Select(ToResourceServerExpression)
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
            .Select(ToResourceServerExpression)
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
            .Select(ToResourceServerExpression)
            .ToListAsync();
    }

    public async Task<List<ResourceServer>> GetAllAsync() =>
        await db.ResourceServers
            .OrderBy(rs => rs.DisplayName)
            .Select(ToResourceServerExpression)
            .ToListAsync();

    private static void ValidateDisplayName(ErrorCollector errors, string displayName)
    {
        if (displayName.Length is < MinDisplayNameLength or > MaxDisplayNameLength)
            errors.Add("displayName", ErrorCode.InvalidLength,
                $"Display name must be {MinDisplayNameLength}-{MaxDisplayNameLength} characters.",
                $"Näyttönimen on oltava {MinDisplayNameLength}–{MaxDisplayNameLength} merkkiä pitkä.");
    }

    private static ValidationException AudienceTaken() =>
        new("audience", ErrorCode.Taken, "Audience is taken.", "Tunniste (Audience) on jo käytössä.");
}