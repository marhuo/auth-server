using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace AuthServer.Web.Application.Tenants;

public partial class TenantService(AuthDbContext db)
{
    public const int MinNameLength = 1;
    public const int MaxNameLength = 200;
    public const int MaxSlugLength = 63;
    public const int MaxSuspensionReasonLength = 500;
    public const int MaxPageSize = 100;

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*\z")]
    private static partial Regex SlugRegex();

    private static readonly HashSet<string> ReservedSlugs = ["admin", "connect", "login", "logout", "api"];

    private static readonly Expression<Func<TenantEntity, Tenant>> ToTenantExpression = e => new Tenant
    {
        Id = e.Id,
        Name = e.Name,
        Slug = e.Slug,
        SuspendedAt = e.SuspendedAt,
        SuspensionReason = e.SuspensionReason,
        CreatedAt = e.CreatedAt
    };

    public static string NormalizeName(string name) =>
        name.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();

    private static string NormalizeSlug(string? slug) => slug?.Trim().ToLowerInvariant() ?? "";

    public async Task<Guid> CreateAsync(string name, string slug)
    {
        name = name?.Trim() ?? "";
        slug = NormalizeSlug(slug);

        var errors = new ErrorCollector();
        ValidateName(errors, name);
        ValidateSlug(errors, slug);
        errors.ThrowIfAny();

        var normalizedName = NormalizeName(name);

        var (nameTaken, slugTaken) = await FindTakenAsync(normalizedName, slug);
        if (nameTaken || slugTaken)
            throw Taken(nameTaken, slugTaken);

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

            var (nameConflict, slugConflict) = await FindTakenAsync(normalizedName, slug);
            if (nameConflict || slugConflict)
                throw Taken(nameConflict, slugConflict);

            throw;
        }

        return tenant.Id;
    }

    public async Task UpdateAsync(Guid id, string name, string slug)
    {
        name = name?.Trim() ?? "";
        slug = NormalizeSlug(slug);

        var errors = new ErrorCollector();
        ValidateName(errors, name);
        ValidateSlug(errors, slug);
        errors.ThrowIfAny();

        var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Id == id)
            ?? throw TenantNotFound(nameof(id));

        var normalizedName = NormalizeName(name);

        var (nameTaken, slugTaken) = await FindTakenAsync(normalizedName, slug, tenant.Id);
        if (nameTaken || slugTaken)
            throw Taken(nameTaken, slugTaken);

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

            var (nameConflict, slugConflict) = await FindTakenAsync(normalizedName, slug, id);
            if (nameConflict || slugConflict)
                throw Taken(nameConflict, slugConflict);

            throw;
        }
    }

    public async Task SuspendAsync(Guid id, string suspensionReason)
    {
        var reason = suspensionReason?.Trim() ?? "";

        var errors = new ErrorCollector();
        if (reason.Length is 0 or > MaxSuspensionReasonLength)
            errors.Add(nameof(suspensionReason), ErrorCode.InvalidLength,
                $"Suspension reason must be 1-{MaxSuspensionReasonLength} characters.",
                $"Keskeytyksen syyn on oltava 1–{MaxSuspensionReasonLength} merkkiä pitkä.");
        errors.ThrowIfAny();

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
            .Select(ToTenantExpression)
            .SingleOrDefaultAsync();

    public async Task<PagedList<Tenant>> GetPagedListAsync(string? searchTerm, int pageNumber, int pageSize)
    {
        var tenants = db.Tenants.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = NormalizeName(searchTerm);
            tenants = tenants.Where(t => t.NormalizedName.Contains(term));
        }

        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = tenants.OrderBy(t => t.NormalizedName).Select(ToTenantExpression);

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
            .Select(ToTenantExpression)
            .ToListAsync();

    private IQueryable<TenantEntity> UserTenantsQuery(Guid userId, Guid resourceServerId, DateTimeOffset currentTime) =>
        db.Tenants
            .AsNoTracking()
            .Where(t => t.SuspendedAt == null
                && t.UserRoles.Any(ur => ur.UserId == userId && ur.Role.ResourceServerId == resourceServerId)
                && t.Subscriptions.Any(s =>
                    s.ResourceServerId == resourceServerId
                    && s.StartsAt <= currentTime
                    && (s.EndsAt == null || s.EndsAt > currentTime)));

    private async Task<(bool Name, bool Slug)> FindTakenAsync(string normalizedName, string slug, Guid? exceptId = null)
    {
        var except = exceptId ?? Guid.Empty;

        var nameTaken = await db.Tenants.AnyAsync(t => t.NormalizedName == normalizedName && t.Id != except);
        var slugTaken = await db.Tenants.AnyAsync(t => t.Slug == slug && t.Id != except);

        return (nameTaken, slugTaken);
    }

    private static void ValidateName(ErrorCollector errors, string name)
    {
        if (name.Length is < MinNameLength or > MaxNameLength)
            errors.Add("name", ErrorCode.InvalidLength,
                $"Name must be {MinNameLength}-{MaxNameLength} characters.",
                $"Nimen on oltava {MinNameLength}–{MaxNameLength} merkkiä pitkä.");
    }

    private static void ValidateSlug(ErrorCollector errors, string slug)
    {
        if (slug.Length is 0 or > MaxSlugLength)
        {
            errors.Add("slug", ErrorCode.InvalidLength,
                $"Slug must be 1-{MaxSlugLength} characters.",
                $"Slugin on oltava 1–{MaxSlugLength} merkkiä pitkä.");
            return;
        }

        if (!SlugRegex().IsMatch(slug))
        {
            errors.Add("slug", ErrorCode.InvalidFormat,
                "Slug may contain only a-z, 0-9 and single hyphens.",
                "Slug saa sisältää vain pieniä kirjaimia a–z, numeroita ja yksittäisiä yhdysmerkkejä.");
            return;
        }

        if (ReservedSlugs.Contains(slug))
            errors.Add("slug", ErrorCode.NotAllowed,
                "Slug is reserved.",
                "Slug on varattu.");
    }

    private static ValidationException Taken(bool name, bool slug)
    {
        var errors = new List<ValidationError>();

        if (name)
            errors.Add(new ValidationError("name", ErrorCode.Taken,
                "Name is taken.", "Nimi on jo käytössä."));

        if (slug)
            errors.Add(new ValidationError("slug", ErrorCode.Taken,
                "Slug is taken.", "Slug on jo käytössä."));

        return new ValidationException(errors);
    }

    private static ValidationException TenantNotFound(string key) =>
        new(Errors.NotFound(key, "Tenant", "Asiakasta"));
}