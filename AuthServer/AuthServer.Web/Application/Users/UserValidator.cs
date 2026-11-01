using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace AuthServer.Web.Application.Users;

public class UserValidator(AuthDbContext db)
{
    private const int MinUserNameLength = 3;
    private const int MaxUserNameLength = 64;
    private const int MaxEmailLength = 254;
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 128;

    public async Task ValidateCreateAsync(string userName, string email, string password, IReadOnlyCollection<UserRole> userRoles)
    {
        var errors = new ErrorCollector();
        ValidateUserName(errors, userName);
        ValidateEmail(errors, email);
        ValidatePassword(errors, password);
        errors.ThrowIfAny();

        await ThrowIfUserNameTakenAsync(UserService.NormalizeUsername(userName));

        foreach (var role in userRoles)
            await ValidateRoleAssignmentAsync(nameof(userRoles), role.RoleId, role.TenantId);
    }

    // user = the current state in the database.
    public async Task ValidateUpdateAsync(UserEntity user, string userName, string email, DateTimeOffset? lockedUntil)
    {
        var errors = new ErrorCollector();
        ValidateUserName(errors, userName);
        ValidateEmail(errors, email);
        errors.ThrowIfAny();

        var now = DateTimeOffset.UtcNow;
        var isLockedNow = user.LockedUntil > now;
        var willBeLocked = lockedUntil > now;

        // Locking a user must not remove the last admin.
        if (willBeLocked && !isLockedNow)
            await ValidateNotLastAdminAsync(user.Id, "lockedUntil");

        var normalized = UserService.NormalizeUsername(userName);
        if (normalized != user.NormalizedUserName)
            await ThrowIfUserNameTakenAsync(normalized, user.Id);
    }

    public Task ValidateDeleteAsync(Guid id) =>
        ValidateNotLastAdminAsync(id, "id");

    public void ValidateChangePassword(string newPassword)
    {
        var errors = new ErrorCollector();
        ValidatePassword(errors, newPassword);
        errors.ThrowIfAny();
    }

    public async Task ValidateRoleAssignmentAsync(string key, Guid roleId, Guid? tenantId)
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
    }

    // Call only for auth server roles.
    public async Task ValidateRemoveAuthServerRoleAsync(Guid userId, Guid roleId)
    {
        var now = DateTimeOffset.UtcNow;

        var hasOtherHolder = await db.UserRoles.AnyAsync(x =>
            x.RoleId == roleId
            && x.UserId != userId
            && (x.User.LockedUntil == null || x.User.LockedUntil <= now));

        if (!hasOtherHolder)
            throw NotAllowed("roleId",
                "The last holder of an auth server role cannot be removed.",
                "Auth server -roolin viimeistä haltijaa ei voi poistaa.");
    }

    public async Task ThrowIfUserNameTakenAsync(string normalizedUserName, Guid? exceptUserId = null)
    {
        var except = exceptUserId ?? Guid.Empty;

        if (await db.TakenUsernames.AnyAsync(x => x.NormalizedUsername == normalizedUserName && x.UserId != except))
            throw new ValidationException(UserNameTakenError());
    }

    private async Task ValidateNotLastAdminAsync(Guid userId, string key)
    {
        var now = DateTimeOffset.UtcNow;

        // The admin roles (auth server roles) that the user holds.
        var adminRoleIds = await db.UserRoles
            .Where(ur => ur.UserId == userId && ur.Role.ResourceServer.IsAuthServer)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        // Every such role must have another holder who is not locked.
        foreach (var roleId in adminRoleIds)
        {
            var hasOtherHolder = await db.UserRoles.AnyAsync(x =>
                x.RoleId == roleId
                && x.UserId != userId
                && (x.User.LockedUntil == null || x.User.LockedUntil <= now));

            if (!hasOtherHolder)
                throw NotAllowed(key,
                    "The last active holder of an admin role cannot be locked or deleted.",
                    "Ylläpitäjäroolin viimeistä aktiivista haltijaa ei voi lukita tai poistaa.");
        }
    }

    public async Task ValidateInviteAsync(string email, Guid tenantId, Guid resourceServerId, IReadOnlyCollection<string> roleNames)
    {
        var errors = new ErrorCollector();

        if (!MailAddress.TryCreate(email, out _))
            errors.Add("email", ErrorCode.InvalidFormat, "Invalid email.", "Virheellinen sähköposti.");

        if (!await db.Tenants.AnyAsync(t => t.Id == tenantId))
            errors.Add("tenantId", ErrorCode.NotFound, "Tenant not found.", "Tenanttia ei löydy.");

        var normalizedNames = roleNames
            .Select(n => n.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();
        var foundNames = await db.Roles
            .Where(r => r.ResourceServerId == resourceServerId && normalizedNames.Contains(r.Name))
            .Select(r => r.Name)
            .ToListAsync();
        var missing = normalizedNames.Except(foundNames).ToList();
        if (missing.Count > 0)
            errors.Add("roleNames", ErrorCode.NotFound,
                $"Unknown roles: {string.Join(", ", missing)}",
                $"Tuntemattomia rooleja: {string.Join(", ", missing)}");

        errors.ThrowIfAny();
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

    private static ValidationException NotAllowed(string key, string en, string fi) =>
        new(key, ErrorCode.NotAllowed, en, fi);
}