using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Text;

namespace AuthServer.Web.Application.Users;

public enum LoginStatus { Success, Failed, LockedOut }

public class UserService(AuthDbContext db, IPasswordHasher<UserEntity> hasher)
{
    public const int MinUserNameLength = 3;
    public const int MaxUserNameLength = 64;
    public const int MaxEmailLength = 254;
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;
    public const int MaxPageSize = 100;

    private const int MaxFailedLogins = 10;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private static readonly Expression<Func<UserEntity, User>> ToUserExpression = e => new User
    {
        Id = e.Id,
        UserName = e.UserName,
        Email = e.Email,
        IsActive = e.IsActive,
        LockedUntil = e.LockedUntil,
        UserRoles = e.UserRoles.Select(ur => new UserRole
        {
            RoleId = ur.RoleId,
            TenantId = ur.TenantId,
        }).ToList()
    };

    private static readonly Expression<Func<UserEntity, UserIdentity>> ToIdentityExpression = e => new UserIdentity
    {
        Id = e.Id,
        UserName = e.UserName,
        Email = e.Email
    };

    public static string NormalizeUsername(string username) =>
        username.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();

    public async Task<Guid> CreateAsync(string userName, string email, string password, IEnumerable<UserRole>? userRoles = null)
    {
        userName = userName?.Trim() ?? "";
        email = email?.Trim() ?? "";
        password ??= "";

        var errors = new ErrorCollector();
        ValidateUserName(errors, userName);
        ValidateEmail(errors, email);
        ValidatePassword(errors, password);
        errors.ThrowIfAny();

        var normalized = NormalizeUsername(userName);

        if (await db.TakenUsernames.AnyAsync(x => x.NormalizedUsername == normalized))
            AddError(errors, UserNameTakenError());

        var assignments = new List<UserRoleEntity>();
        var seen = new HashSet<(Guid RoleId, Guid? TenantId)>();

        foreach (var requested in userRoles ?? [])
        {
            if (!seen.Add((requested.RoleId, requested.TenantId)))
                continue;

            await EnsureRoleCanBeAssignedAsync(nameof(userRoles), requested.RoleId, requested.TenantId);
            assignments.Add(new UserRoleEntity
            {
                Id = Guid.CreateVersion7(),
                RoleId = requested.RoleId,
                TenantId = requested.TenantId
            });
        }

        errors.ThrowIfAny();

        var user = new UserEntity
        {
            Id = Guid.CreateVersion7(),
            UserName = userName,
            NormalizedUserName = normalized,
            Email = email
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        user.TakenUsernames.Add(new TakenUsernameEntity
        {
            Id = Guid.CreateVersion7(),
            NormalizedUsername = normalized
        });

        foreach (var assignment in assignments)
            user.UserRoles.Add(assignment);

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            if (await db.TakenUsernames.AnyAsync(x => x.NormalizedUsername == normalized))
                throw new ValidationException(UserNameTakenError());

            throw;
        }

        return user.Id;
    }

    public async Task<(LoginStatus Status, UserIdentity? User)> CheckPasswordAsync(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
            return (LoginStatus.Failed, null);

        var normalized = NormalizeUsername(userName);
        var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedUserName == normalized);

        if (user is null || !user.IsActive) return (LoginStatus.Failed, null);
        if (user.LockedUntil > DateTimeOffset.UtcNow) return (LoginStatus.LockedOut, null);

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);

        if (result == PasswordVerificationResult.Failed)
        {
            if (++user.FailedLogins >= MaxFailedLogins)
            {
                user.LockedUntil = DateTimeOffset.UtcNow.Add(LockoutDuration);
                user.FailedLogins = 0;
            }

            await db.SaveChangesAsync();
            return (LoginStatus.Failed, null);
        }

        user.FailedLogins = 0;
        user.LockedUntil = null;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = hasher.HashPassword(user, password);

        await db.SaveChangesAsync();

        return (LoginStatus.Success, new UserIdentity
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email
        });
    }

    public Task<UserIdentity?> GetActiveIdentityAsync(Guid userId) =>
        db.Users
            .Where(u => u.Id == userId && u.IsActive)
            .Select(ToIdentityExpression)
            .SingleOrDefaultAsync();

    public Task<bool> IsActiveAsync(Guid userId) => db.Users.AnyAsync(u => u.Id == userId && u.IsActive);

    public Task<Guid?> GetUserIdByNameAsync(string userName)
    {
        var normalized = NormalizeUsername(userName);

        return db.Users
            .Where(u => u.NormalizedUserName == normalized)
            .Select(u => (Guid?)u.Id)
            .SingleOrDefaultAsync();
    }

    public Task<User?> GetByIdAsync(Guid id) =>
        db.Users
            .Where(u => u.Id == id)
            .Select(ToUserExpression)
            .SingleOrDefaultAsync();

    public async Task<PagedList<User>> GetPagedListAsync(string? searchTerm, int pageNumber, int pageSize)
    {
        var users = db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            var normalized = NormalizeUsername(term);
            var pattern = $"%{EscapeLike(term)}%";

            users = users.Where(u =>
                u.NormalizedUserName.Contains(normalized) ||
                EF.Functions.ILike(u.Email, pattern));
        }

        return await PageAsync(
            users.OrderBy(u => u.NormalizedUserName).Select(ToUserExpression),
            pageNumber,
            pageSize);
    }

    private static async Task<PagedList<T>> PageAsync<T>(IQueryable<T> query, int pageNumber, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        pageNumber = Math.Clamp(pageNumber, 1, totalPages);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedList<T>(items, pageNumber, pageSize, totalCount);
    }

    public Task<List<string>> GetAuthServerRolesAsync(Guid userId) =>
        db.UserRoles
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Role.ResourceServer.IsAuthServer)
            .Select(x => x.Role.Name)
            .ToListAsync();

    public Task<List<string>> GetTenantRolesAsync(Guid userId, Guid tenantId) =>
        db.UserRoles
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.TenantId == tenantId)
            .Select(x => x.Role.Name)
            .ToListAsync();

    public async Task AddAuthServerRoleAsync(Guid userId, Guid roleId)
    {
        await EnsureUserExistsAsync(userId, nameof(userId));

        await EnsureRoleCanBeAssignedAsync(nameof(roleId), roleId, null);
        await AddRoleAsync(userId, null, roleId);
    }

    public async Task AddTenantRoleAsync(Guid userId, Guid tenantId, Guid roleId)
    {
        await EnsureUserExistsAsync(userId, nameof(userId));

        await EnsureRoleCanBeAssignedAsync(nameof(roleId), roleId, tenantId);
        await AddRoleAsync(userId, tenantId, roleId);
    }

    public Task RemoveTenantRoleAsync(Guid userId, Guid tenantId, Guid roleId) =>
        db.UserRoles
            .Where(x => x.UserId == userId && x.RoleId == roleId && x.TenantId == tenantId)
            .ExecuteDeleteAsync();

    public async Task RemoveAuthServerRoleAsync(Guid userId, Guid roleId)
    {
        var assignmentId = await db.UserRoles
            .Where(x => x.UserId == userId && x.RoleId == roleId && x.Role.ResourceServer.IsAuthServer)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync();

        if (assignmentId is null)
            return;

        var hasOtherHolder = await db.UserRoles.AnyAsync(x =>
            x.RoleId == roleId
            && x.UserId != userId
            && x.User.IsActive);

        if (!hasOtherHolder)
            throw new ValidationException(nameof(roleId), ErrorCode.NotAllowed,
                "The last holder of an auth server role cannot be removed.",
                "Auth server -roolin viimeistä haltijaa ei voi poistaa.");

        await db.UserRoles.Where(x => x.Id == assignmentId).ExecuteDeleteAsync();
    }

    private async Task AddRoleAsync(Guid userId, Guid? tenantId, Guid roleId)
    {
        if (await AssignmentExistsAsync(userId, roleId, tenantId))
            return;

        db.UserRoles.Add(new UserRoleEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            RoleId = roleId,
            TenantId = tenantId
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            if (await AssignmentExistsAsync(userId, roleId, tenantId))
                return;

            throw;
        }
    }

    private Task<bool> AssignmentExistsAsync(Guid userId, Guid roleId, Guid? tenantId) =>
        db.UserRoles.AnyAsync(x => x.UserId == userId && x.RoleId == roleId && x.TenantId == tenantId);

    private async Task EnsureRoleCanBeAssignedAsync(string key, Guid roleId, Guid? tenantId)
    {
        var role = await db.Roles.AsNoTracking()
            .Include(r => r.ResourceServer)
            .SingleOrDefaultAsync(r => r.Id == roleId)
            ?? throw new ValidationException(Errors.NotFound(key, "Role", "Roolia"));

        if (role.ResourceServer.IsAuthServer)
        {
            if (tenantId is not null)
                throw NotAllowed(key, "An auth server role cannot belong to a tenant.",
                    "Auth server -rooli ei voi kuulua asiakkaalle.");

            return;
        }

        if (tenantId is not { } id)
            throw NotAllowed(key, "A tenant role needs a tenant.",
                "Asiakaskohtainen rooli vaatii asiakkaan.");

        if (!await db.Tenants.AnyAsync(t => t.Id == id))
            throw new ValidationException(Errors.NotFound(key, "Tenant", "Asiakasta"));

        var now = DateTimeOffset.UtcNow;

        var hasSubscription = await db.Subscriptions.AnyAsync(s =>
            s.TenantId == id
            && s.ResourceServerId == role.ResourceServerId
            && s.StartsAt <= now
            && (s.EndsAt == null || s.EndsAt > now));

        if (!hasSubscription)
            throw NotAllowed(key, "The tenant has no active subscription for the product of this role.",
                "Asiakkaalla ei ole voimassa olevaa tilausta tämän roolin tuotteeseen.");
    }


    private async Task EnsureUserExistsAsync(Guid userId, string key)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId))
            throw new ValidationException(Errors.NotFound(key, "User", "Käyttäjää"));
    }

    private static void ValidateUserName(ErrorCollector errors, string userName)
    {
        if (userName.Length is < MinUserNameLength or > MaxUserNameLength)
        {
            errors.Add("userName", ErrorCode.InvalidLength,
                $"User name must be {MinUserNameLength}-{MaxUserNameLength} characters.",
                $"Käyttäjänimen on oltava {MinUserNameLength}–{MaxUserNameLength} merkkiä pitkä.");
            return;
        }

        if (userName.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
        {
            errors.Add("userName", ErrorCode.InvalidFormat,
                "User name must not contain whitespace or control characters.",
                "Käyttäjänimi ei saa sisältää välilyöntejä tai ohjausmerkkejä.");
        }
    }

    private static void ValidateEmail(ErrorCollector errors, string email)
    {
        if (email.Length is 0 or > MaxEmailLength)
        {
            errors.Add("email", ErrorCode.InvalidLength,
                $"Email must be 1-{MaxEmailLength} characters.",
                $"Sähköpostin on oltava 1–{MaxEmailLength} merkkiä pitkä.");
            return;
        }

        if (!MailAddress.TryCreate(email, out var address) || address.Address != email)
        {
            errors.Add("email", ErrorCode.InvalidFormat,
                "Email address is not valid.",
                "Sähköpostiosoite ei ole kelvollinen.");
        }
    }

    private static void ValidatePassword(ErrorCollector errors, string password)
    {
        if (password.Length is < MinPasswordLength or > MaxPasswordLength)
        {
            errors.Add("password", ErrorCode.InvalidLength,
                $"Password must be {MinPasswordLength}-{MaxPasswordLength} characters.",
                $"Salasanan on oltava {MinPasswordLength}–{MaxPasswordLength} merkkiä pitkä.");
        }
    }

    private static ValidationError UserNameTakenError() =>
        new("userName", ErrorCode.Taken,
            "User name is taken or has been used before.",
            "Käyttäjänimi on jo käytössä tai se on ollut aiemmin käytössä.");

    private static void AddError(ErrorCollector errors, ValidationError e) =>
        errors.Add(e.Key, e.Code, e.MessageEn, e.MessageFi);

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static ValidationException NotAllowed(string key, string en, string fi) =>
        new(key, ErrorCode.NotAllowed, en, fi);
}