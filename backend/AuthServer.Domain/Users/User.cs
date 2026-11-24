using System.Net.Mail;
using System.Text.Json.Serialization;

namespace AuthServer.Domain.Users;

// LockedOut: automatic lockout after failed logins. Locked: locked by an admin.
public enum LoginStatus { Success, Failed, LockedOut, Locked }

// Removing access means removing roles, and a person can be invited again. Only a user who never
// became Active is deleted (IUserRepository.DeleteUnusedInvitedAsync).
public sealed class User : AggregateRoot
{
    public const int MaxEmailLength = 254;
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    // An Invited user without roles is deleted this long after creation (registration not finished).
    public static readonly TimeSpan UnusedInvitedRetention = TimeSpan.FromDays(30);

    public const int MaxFailedLogins = 10;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // Error key used for both the create password and the new password.
    private const string PasswordKey = "password";

    private readonly List<UserRole> _roles;
    private readonly List<UserExternalId> _externalIds;

    [JsonConstructor]
    public User(
        Guid id,
        string email,
        string normalizedEmail,
        string? passwordHash,
        int failedLogins,
        DateTimeOffset? lockedUntil,
        bool locked,
        UserStatuses status,
        IReadOnlyList<UserRole> roles,
        IReadOnlyList<UserExternalId> externalIds,
        DateTimeOffset createdAt)
        : base(id)
    {
        Email = email;
        NormalizedEmail = normalizedEmail;
        PasswordHash = passwordHash;
        FailedLogins = failedLogins;
        LockedUntil = lockedUntil;
        Locked = locked;
        Status = status;
        _roles = [.. roles];
        _externalIds = [.. externalIds];
        CreatedAt = createdAt;
    }

    // The login identifier.
    public string Email { get; private set; }

    // Uniqueness and lookup key, see NormalizeEmail. Unique among current users only: a changed email
    // is free for others. Identify users by Id, not by email.
    public string NormalizedEmail { get; private set; }

    // Null until the invited user sets a password, and for users who sign in only externally.
    public string? PasswordHash { get; private set; }

    public int FailedLogins { get; private set; }

    // Automatic lockout after too many failed logins (see Login). Ends by itself.
    public DateTimeOffset? LockedUntil { get; private set; }

    // Locked by an admin. Lasts until an admin unlocks the user.
    public bool Locked { get; private set; }

    public IReadOnlyList<UserRole> Roles => _roles;

    // Accounts in external sign-in services, e.g. Google.
    public IReadOnlyList<UserExternalId> ExternalIds => _externalIds;

    public DateTimeOffset CreatedAt { get; private set; }

    [JsonIgnore]
    public bool HasPassword => PasswordHash is not null;

    // Invited until the user has a way to sign in: a password (SetPassword) or an external account
    // (AddExternalId). Stored, so never-activated users can be found in the database.
    public UserStatuses Status { get; private set; }

    // Whether the automatic lockout is in force.
    public bool IsLockedOut(DateTimeOffset now) => LockedUntil > now;

    // Checked at login, at the authorize and token endpoints and when the sign-in cookie is validated.
    public bool CanSignIn(DateTimeOffset now) => !Locked && !IsLockedOut(now);

    // Users are created only by invitation: the new user is Invited (UserStatuses) and has no password
    // until they set it with the invitation link.
    public static User CreateInvited(string email, DateTimeOffset now)
    {
        email = email.Trim();

        var errors = new ErrorCollector();
        ValidateEmail(errors, email);
        errors.ThrowIfAny();

        return new User(
            id: Guid.CreateVersion7(),
            email: email,
            normalizedEmail: NormalizeEmail(email),
            passwordHash: null,
            failedLogins: 0,
            lockedUntil: null,
            locked: false,
            status: UserStatuses.Invited,
            roles: [],
            externalIds: [],
            createdAt: now);
    }

    // A user who signs in with an external service for the first time. Active at once.
    public static User CreateExternal(ExternalIdentity identity, DateTimeOffset now)
    {
        var user = CreateInvited(email: identity.Email, now: now);
        user.AddExternalId(identity, now);
        return user;
    }

    public void SetEmail(string email)
    {
        email = email.Trim();

        var errors = new ErrorCollector();
        ValidateEmail(errors, email);
        errors.ThrowIfAny();

        Email = email;
        NormalizedEmail = NormalizeEmail(email);
    }

    // Lock or unlock by an admin. Unlocking also ends the automatic lockout and resets the failed
    // login counter, so the admin can let a user back in at once.
    public void SetLocked(bool locked)
    {
        Locked = locked;

        if (!locked)
        {
            LockedUntil = null;
            FailedLogins = 0;
        }
    }

    // Also ends the automatic lockout: a new password (e.g. after a password reset) lets the user sign
    // in again. The admin lock (Locked) is not changed.
    public void SetPassword(string password, IPasswordHasher hasher)
    {
        var errors = new ErrorCollector();
        ValidatePassword(errors, password);
        errors.ThrowIfAny();

        PasswordHash = hasher.Hash(password);
        LockedUntil = null;
        FailedLogins = 0;
        Status = UserStatuses.Active;
    }

    // Checks the password and applies the lockout policy: after MaxFailedLogins failed attempts
    // the user is locked out for LockoutDuration. A successful login resets the counter and rehashes
    // the password when the hash algorithm has changed. The admin lock is reported only after a
    // correct password, so it does not reveal anything to someone who does not know the password.
    public LoginStatus Login(string password, IPasswordHasher hasher, DateTimeOffset now)
    {
        if (IsLockedOut(now))
            return LoginStatus.LockedOut;

        var check = PasswordHash is null ? PasswordCheck.Failed : hasher.Verify(PasswordHash, password);

        if (check == PasswordCheck.Failed)
        {
            if (++FailedLogins >= MaxFailedLogins)
            {
                LockedUntil = now.Add(LockoutDuration);
                FailedLogins = 0;
            }

            return LoginStatus.Failed;
        }

        FailedLogins = 0;
        LockedUntil = null;

        if (check == PasswordCheck.SuccessRehashNeeded)
            PasswordHash = hasher.Hash(password);

        return Locked ? LoginStatus.Locked : LoginStatus.Success;
    }

    public bool HasRole(Guid roleId, Guid? tenantId) =>
        _roles.Any(r => r.RoleId == roleId && r.TenantId == tenantId);

    // Whether the role/tenant combination is valid is checked by UserValidator (needs repositories).
    public void AddRole(Guid roleId, Guid? tenantId, DateTimeOffset now)
    {
        if (HasRole(roleId, tenantId))
            return;

        _roles.Add(new UserRole(roleId, tenantId, now));
    }

    public void RemoveRole(Guid roleId, Guid? tenantId)
    {
        _roles.RemoveAll(r => r.RoleId == roleId && r.TenantId == tenantId);
    }

    // Removes the given roles in one tenant. Returns whether the user had any of them.
    public bool RemoveRolesInTenant(IReadOnlyCollection<Guid> roleIds, Guid tenantId) =>
        _roles.RemoveAll(r => r.TenantId == tenantId && roleIds.Contains(r.RoleId)) > 0;

    public bool HasExternalId(string provider, string providerUserId) =>
        _externalIds.Any(x => x.Provider == provider && x.ProviderUserId == providerUserId);

    public void AddExternalId(ExternalIdentity identity, DateTimeOffset now)
    {
        if (HasExternalId(identity.Provider, identity.ProviderUserId))
            return;

        _externalIds.Add(new UserExternalId(identity.Provider, identity.ProviderUserId, now));
        Status = UserStatuses.Active;
    }

    // Expects a trimmed value.
    public static void ValidateEmail(ErrorCollector errors, string email)
    {
        if (email.Length is 0 or > MaxEmailLength)
        {
            errors.Add(nameof(Email), ErrorCode.InvalidLength,
                $"Email must be 1-{MaxEmailLength} characters.",
                $"Sähköpostin on oltava 1–{MaxEmailLength} merkkiä pitkä.");
            return;
        }

        if (!MailAddress.TryCreate(email, out var address) || address.Address != email)
        {
            errors.Add(nameof(Email), ErrorCode.InvalidFormat,
                "Email address is not valid.",
                "Sähköpostiosoite ei ole kelvollinen.");
        }
    }

    public static void ValidatePassword(ErrorCollector errors, string password)
    {
        if (password.Length is < MinPasswordLength or > MaxPasswordLength)
        {
            errors.Add(PasswordKey, ErrorCode.InvalidLength,
                $"Password must be {MinPasswordLength}-{MaxPasswordLength} characters.",
                $"Salasanan on oltava {MinPasswordLength}–{MaxPasswordLength} merkkiä pitkä.");
        }
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static ValidationError EmailTakenError() =>
        new(nameof(Email), ErrorCode.Taken,
            "Email is used by another user.",
            "Sähköposti on toisen käyttäjän käytössä.");
}
