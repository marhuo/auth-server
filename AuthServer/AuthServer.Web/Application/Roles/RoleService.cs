using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace AuthServer.Web.Application.Roles;

public partial class RoleService(AuthDbContext db)
{
    public const int MinNameLength = 2;
    public const int MaxNameLength = 64;
    public const int MaxDescriptionLength = 300;
    public const int MaxPageSize = 100;

    [GeneratedRegex(@"^[a-z0-9][a-z0-9._:-]*\z")]
    private static partial Regex RoleNameRegex();

    private static readonly Expression<Func<RoleEntity, Role>> ToRoleExpression = e => new Role
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        ResourceServerId = e.ResourceServerId
    };

    public async Task<Guid?> GetIdByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalized = name.Trim().ToLowerInvariant();

        return await db.Roles
            .Where(r => r.Name == normalized)
            .Select(r => (Guid?)r.Id)
            .SingleOrDefaultAsync();
    }

    public async Task<Guid> CreateAsync(string name, string description, Guid resourceServerId)
    {
        name = name?.Trim() ?? "";
        description = NormalizeDescription(description);

        var errors = new ErrorCollector();
        ValidateName(errors, name);
        ValidateDescription(errors, description);
        errors.ThrowIfAny();

        if (!await db.ResourceServers.AnyAsync(r => r.Id == resourceServerId))
            throw new ValidationException(
                Errors.NotFound(nameof(resourceServerId), "Resource server", "Resurssipalvelinta"));

        if (await db.Roles.AnyAsync(r => r.Name == name))
            throw NameTaken();

        var role = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            ResourceServerId = resourceServerId,
            Name = name,
            Description = description
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
                throw NameTaken();

            throw;
        }

        return role.Id;
    }

    public async Task UpdateAsync(Guid id, string name, string description, Guid resourceServerId)
    {
        name = name?.Trim() ?? "";
        description = NormalizeDescription(description);

        var errors = new ErrorCollector();
        ValidateName(errors, name);
        ValidateDescription(errors, description);
        errors.ThrowIfAny();

        var role = await db.Roles.SingleOrDefaultAsync(r => r.Id == id)
            ?? throw new ValidationException(Errors.NotFound(nameof(id), "Role", "Roolia"));

        if (!await db.ResourceServers.AnyAsync(r => r.Id == resourceServerId))
            throw new ValidationException(
                Errors.NotFound(nameof(resourceServerId), "Resource server", "Resurssipalvelinta"));

        var assigned = await db.UserRoles.AnyAsync(ur => ur.RoleId == role.Id);

        if (assigned && role.ResourceServerId != resourceServerId)
            throw new ValidationException(nameof(resourceServerId), ErrorCode.NotAllowed,
                "The resource server cannot be changed while the role is assigned to users.",
                "Resurssipalvelinta ei voi vaihtaa, kun rooli on annettu käyttäjille.");

        if (assigned && role.Name != name)
            throw new ValidationException(nameof(name), ErrorCode.NotAllowed,
                "The name cannot be changed while the role is assigned to users.",
                "Nimeä ei voi vaihtaa, kun rooli on annettu käyttäjille.");

        if (await db.Roles.AnyAsync(r => r.Name == name && r.Id != role.Id))
            throw NameTaken();

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
                throw NameTaken();

            throw;
        }
    }

    public Task<Role?> GetByIdAsync(Guid id) =>
        db.Roles
            .Where(r => r.Id == id)
            .Select(ToRoleExpression)
            .SingleOrDefaultAsync();

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

        var query = roles.OrderBy(r => r.Name).Select(ToRoleExpression);

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        pageNumber = Math.Clamp(pageNumber, 1, totalPages);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedList<Role>(items, pageNumber, pageSize, totalCount);
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

        if (!RoleNameRegex().IsMatch(name))
        {
            errors.Add("name", ErrorCode.InvalidFormat,
                "Name may contain only lowercase letters a-z, digits and the characters . _ : -",
                "Nimi saa sisältää vain pieniä kirjaimia a–z, numeroita sekä merkit . _ : -");
        }
    }

    private static void ValidateDescription(ErrorCollector errors, string description)
    {
        if (description.Length > MaxDescriptionLength)
        {
            errors.Add("description", ErrorCode.InvalidLength,
                $"Description must be at most {MaxDescriptionLength} characters.",
                $"Kuvauksen on oltava enintään {MaxDescriptionLength} merkkiä pitkä.");
        }
    }

    private static string NormalizeDescription(string? description) => description?.Trim() ?? string.Empty;

    private static ValidationException NameTaken() =>
        new("name", ErrorCode.Taken, "Name is taken.", "Nimi on jo käytössä.");

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}