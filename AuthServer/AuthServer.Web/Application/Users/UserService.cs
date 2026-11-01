using AuthServer.Web.Application.Email;
using AuthServer.Web.Application.Roles;
using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text;

namespace AuthServer.Web.Application.Users;

public enum LoginStatus { Success, Failed, LockedOut }

public class UserService(AuthDbContext db,
    RoleService roleService,
    UserTokenService tokenService,
    IUserEmailSender emailSender,
    IPasswordHasher<UserEntity> hasher, 
    UserValidator validator)
{
    public const int MaxPageSize = 100;

    private const int MaxFailedLogins = 10;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public static string NormalizeUsername(string username) =>
        username.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static readonly Expression<Func<UserEntity, UserIdentity>> ToIdentityExpression = e => new UserIdentity
    {
        Id = e.Id,
        UserName = e.UserName,
        Email = e.Email
    };

    public Task<UserIdentity?> GetIdentityAsync(Guid userId) =>
      db.Users
          .Where(u => u.Id == userId)
          .Select(ToIdentityExpression)
          .SingleOrDefaultAsync();

    public async Task<Guid> CreateAsync(string userName, string email, string password, IEnumerable<UserRole>? userRoles = null)
    {
        userName = userName?.Trim() ?? "";
        email = email?.Trim() ?? "";
        password ??= "";

        var roles = (userRoles ?? []).DistinctBy(r => (r.RoleId, r.TenantId)).ToList();

        await validator.ValidateCreateAsync(userName, email, password, roles);

        return await CreateCoreAsync(userName, email, password, roles);
    }

    private async Task<Guid> CreateCoreAsync(string userName, string email, string? password, List<UserRole> roles)
    {
        var normalized = NormalizeUsername(userName);
        var now = DateTimeOffset.UtcNow;

        var user = new UserEntity
        {
            Id = Guid.CreateVersion7(),
            UserName = userName,
            NormalizedUserName = normalized,
            Email = email,
            NormalizedEmail = NormalizeEmail(email),
            CreatedAt = now
        };

        if (password is not null)
            user.PasswordHash = hasher.HashPassword(user, password);

        user.TakenUsernames.Add(new TakenUsernameEntity
        {
            Id = Guid.CreateVersion7(),
            NormalizedUsername = normalized,
            CreatedAt = now
        });

        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRoleEntity
            {
                Id = Guid.CreateVersion7(),
                RoleId = role.RoleId,
                TenantId = role.TenantId,
                CreatedAt = now
            });
        }

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            await validator.ThrowIfUserNameTakenAsync(normalized);
            throw;
        }

        return user.Id;
    }

    public async Task UpdateAsync(Guid id, string userName, string email, DateTimeOffset? lockedUntil)
    {
        userName = userName?.Trim() ?? "";
        email = email?.Trim() ?? "";

        var user = await FindUserAsync(id);

        await validator.ValidateUpdateAsync(user, userName, email, lockedUntil);

        var normalized = NormalizeUsername(userName);

        if (normalized != user.NormalizedUserName
            && !await db.TakenUsernames.AnyAsync(x => x.NormalizedUsername == normalized))
        {
            db.TakenUsernames.Add(new TakenUsernameEntity
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                NormalizedUsername = normalized,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        user.UserName = userName;
        user.NormalizedUserName = normalized;
        user.Email = email;
        user.NormalizedEmail = NormalizeEmail(email);
        user.LockedUntil = lockedUntil?.ToUniversalTime();

        if (user.LockedUntil is null)
            user.FailedLogins = 0;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            await validator.ThrowIfUserNameTakenAsync(normalized, id);

            throw;
        }
    }

    public async Task ChangePasswordAsync(Guid id, string newPassword)
    {
        newPassword ??= "";

        validator.ValidateChangePassword(newPassword);

        var user = await FindUserAsync(id);

        user.PasswordHash = hasher.HashPassword(user, newPassword);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;

        await db.RefreshTokens
            .Where(t => t.UserId == id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
    }

    public async Task DeleteAsync(Guid id)
    {
        if (!await db.Users.AnyAsync(u => u.Id == id))
            throw UserNotFound(nameof(id));

        await validator.ValidateDeleteAsync(id);

        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.UserRoles.Where(x => x.UserId == id).ExecuteDeleteAsync();
        await db.RefreshTokens.Where(x => x.UserId == id).ExecuteDeleteAsync();
        await db.AuthorizationCodes.Where(x => x.UserId == id).ExecuteDeleteAsync();
        await db.TakenUsernames.Where(u => u.UserId == id).ExecuteDeleteAsync();
        await db.Users.Where(u => u.Id == id).ExecuteDeleteAsync();

        await transaction.CommitAsync();
    }

    public async Task<(LoginStatus Status, UserIdentity? User)> CheckPasswordAsync(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
            return (LoginStatus.Failed, null);

        var normalized = NormalizeUsername(userName);
        var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedUserName == normalized);

        if (user is null) return (LoginStatus.Failed, null);
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
            .Select(UserMapper.ToDomainExpression)
            .SingleOrDefaultAsync();

    public Task<User?> GetByEmailAsync(string email)
    {
        email = NormalizeEmail(email);
        return db.Users
            .Where(u => u.Email == email)
            .Select(UserMapper.ToDomainExpression)
            .SingleOrDefaultAsync();
    }

    public async Task<List<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
            return [];

        var distinctIds = ids.Distinct().ToList();

        return await db.Users
            .Where(u => distinctIds.Contains(u.Id))
            .OrderBy(u => u.NormalizedUserName)
            .Select(UserMapper.ToDomainExpression)
            .ToListAsync();
    }

    public async Task<PagedList<User>> GetPagedListAsync(string? searchTerm, int pageNumber, int pageSize)
    {
        var users = db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{EscapeLike(searchTerm.Trim())}%";
            users = users.Where(u =>
                EF.Functions.ILike(u.NormalizedUserName, pattern) ||
                EF.Functions.ILike(u.NormalizedEmail, pattern));
        }

        return await PageAsync(
            users.OrderBy(u => u.NormalizedUserName).Select(UserMapper.ToDomainExpression),
            pageNumber,
            pageSize);
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

    public async Task AddRoleAsync(Guid userId, Guid roleId, Guid? tenantId)
    {
        await EnsureUserExistsAsync(userId, nameof(userId));
        await validator.ValidateRoleAssignmentAsync(nameof(roleId), roleId, tenantId);

        if (await HasRoleAsync(userId, roleId, tenantId))
            return;

        db.UserRoles.Add(new UserRoleEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            RoleId = roleId,
            TenantId = tenantId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            if (await HasRoleAsync(userId, roleId, tenantId))
                return;

            throw;
        }
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId, Guid? tenantId)
    {
        var assignment = await db.UserRoles
            .Where(x => x.UserId == userId && x.RoleId == roleId && x.TenantId == tenantId)
            .Select(x => new { x.Id, x.Role.ResourceServer.IsAuthServer })
            .SingleOrDefaultAsync();

        if (assignment is null)
            return;

        if (assignment.IsAuthServer)
            await validator.ValidateRemoveAuthServerRoleAsync(userId, roleId);

        await db.UserRoles.Where(x => x.Id == assignment.Id).ExecuteDeleteAsync();
    }

    public Task<bool> HasRoleAsync(Guid userId, Guid roleId, Guid? tenantId) =>
        db.UserRoles.AnyAsync(x => x.UserId == userId && x.RoleId == roleId && x.TenantId == tenantId);

    private async Task<UserEntity> FindUserAsync(Guid id) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == id)
        ?? throw UserNotFound(nameof(id));

    private async Task EnsureUserExistsAsync(Guid userId, string key)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId))
            throw UserNotFound(key);
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

    private static ValidationException UserNotFound(string key) =>
        new(Errors.NotFound(key, "User", "Käyttäjää"));

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    public async Task<Guid> InviteAsync(string email, Guid tenantId, Guid resourceServerId, IReadOnlyCollection<string> roleNames)
    {
        var normalized = NormalizeEmail(email);
        await validator.ValidateInviteAsync(email, tenantId, resourceServerId, roleNames);

        var roleIds = (await roleService.GetIdsByNamesAsync(resourceServerId, roleNames)).Values;

        var user = await GetByEmailAsync(normalized);
        if (user is null)
        {
            var id = await CreateCoreAsync(normalized, email.Trim(), null, []);
            user = (await GetByIdAsync(id))!;
        }

        foreach (var roleId in roleIds)
            if (!await HasRoleAsync(user.Id, roleId, tenantId))
                await AddRoleAsync(user.Id, roleId, tenantId);

        if (!user.HasPassword)
        {
            var token = await tokenService.CreateInviteTokenAsync(user.Id);
            await emailSender.SendInviteAsync(user.Email, token);
        }
        return user.Id;
    }
}