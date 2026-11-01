using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace AuthServer.Web.Application.Scopes;

public partial class ScopeService(AuthDbContext db)
{
    public const int MinNameLength = 4;
    public const int MaxNameLength = 200;
    public const int MinDisplayNameLength = 4;
    public const int MaxDisplayNameLength = 200;

    [GeneratedRegex(@"^[a-z0-9][a-z0-9._:-]*\z")]
    private static partial Regex ScopeNameRegex();

    private static readonly HashSet<string> ReservedNames =
        ["openid", "profile", "email", "address", "phone", "offline_access"];

    private static readonly Expression<Func<ScopeEntity, Scope>> ToScopeExpression = e => new Scope
    {
        Id = e.Id,
        Name = e.Name,
        DisplayName = e.DisplayName,
        ResourceServerId = e.ResourceServerId
    };

    public async Task<Guid> CreateAsync(string name, string displayName, Guid? resourceServerId)
    {
        name = name?.Trim() ?? "";
        displayName = displayName?.Trim() ?? "";

        var errors = new ErrorCollector();
        ValidateName(errors, name);
        ValidateDisplayName(errors, displayName);
        errors.ThrowIfAny();

        if (resourceServerId is { } id && !await db.ResourceServers.AnyAsync(r => r.Id == id))
            throw new ValidationException(
                Errors.NotFound(nameof(resourceServerId), "Resource server", "Resurssipalvelinta"));

        if (await db.Scopes.AnyAsync(s => s.Name == name))
            throw NameTaken();

        var scope = new ScopeEntity
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            DisplayName = displayName,
            ResourceServerId = resourceServerId
        };

        db.Scopes.Add(scope);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            if (await db.Scopes.AnyAsync(s => s.Name == name))
                throw NameTaken();

            throw;
        }

        return scope.Id;
    }

    public async Task UpdateAsync(Guid id, string displayName)
    {
        displayName = displayName?.Trim() ?? "";

        var errors = new ErrorCollector();
        ValidateDisplayName(errors, displayName);
        errors.ThrowIfAny();

        var scope = await db.Scopes.SingleOrDefaultAsync(s => s.Id == id)
            ?? throw new ValidationException(Errors.NotFound(nameof(id), "Scope", "Scopea"));

        scope.DisplayName = displayName;
        await db.SaveChangesAsync();
    }

    public Task<Scope?> GetByIdAsync(Guid id) =>
        db.Scopes
            .Where(s => s.Id == id)
            .Select(ToScopeExpression)
            .SingleOrDefaultAsync();

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
            .Select(ToScopeExpression)
            .ToPagedList(pageNumber, pageSize));
    }

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

    private static void ValidateName(ErrorCollector errors, string name)
    {
        if (name.Length is < MinNameLength or > MaxNameLength)
        {
            errors.Add("name", ErrorCode.InvalidLength,
                $"Name must be {MinNameLength}-{MaxNameLength} characters.",
                $"Nimen on oltava {MinNameLength}–{MaxNameLength} merkkiä pitkä.");
            return;
        }

        if (!ScopeNameRegex().IsMatch(name))
        {
            errors.Add("name", ErrorCode.InvalidFormat,
                "Name may contain only lowercase letters a-z, digits and the characters . _ : -",
                "Nimi saa sisältää vain pieniä kirjaimia a–z, numeroita sekä merkit . _ : -");
            return;
        }

        if (ReservedNames.Contains(name))
        {
            errors.Add("name", ErrorCode.NotAllowed,
                "Name is reserved.",
                "Nimi on varattu.");
        }
    }

    private static void ValidateDisplayName(ErrorCollector errors, string displayName)
    {
        if (displayName.Length is < MinDisplayNameLength or > MaxDisplayNameLength)
        {
            errors.Add("displayName", ErrorCode.InvalidLength,
                $"Display name must be {MinDisplayNameLength}-{MaxDisplayNameLength} characters.",
                $"Näyttönimen on oltava {MinDisplayNameLength}–{MaxDisplayNameLength} merkkiä pitkä.");
        }
    }

    private static ValidationException NameTaken() =>
        new("name", ErrorCode.Taken, "Name is taken.", "Nimi on jo käytössä.");

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}