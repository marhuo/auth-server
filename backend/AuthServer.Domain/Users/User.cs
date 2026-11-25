using System.Net.Mail;
using System.Text.Json.Serialization;

namespace AuthServer.Domain.Users;

// Signs in with a code sent by email (SignInCode) or with an external service such as Google; there
// are no passwords. Removing access means removing roles, and a person can be invited again. Only a
// user who never became Active is deleted (IUserRepository.DeleteUnusedInvitedAsync).
public sealed class User : AggregateRoot
{
    public const int MaxEmailLength = 254;

    // An Invited user without roles is deleted this long after creation.
    public static readonly TimeSpan UnusedInvitedRetention = TimeSpan.FromDays(30);

    private readonly List<UserRole> _roles;
    private readonly List<UserExternalId> _externalIds;

    [JsonConstructor]
    public User(
        Guid id,
        string email,
        string normalizedEmail,
        bool locked,
        UserStatuses status,
        IReadOnlyList<UserRole> roles,
        IReadOnlyList<UserExternalId> externalIds,
        DateTimeOffset createdAt)
        : base(id)
    {
        Email = email;
        NormalizedEmail = normalizedEmail;
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

    // Locked by an admin. Lasts until an admin unlocks the user.
    public bool Locked { get; private set; }

    public IReadOnlyList<UserRole> Roles => _roles;

    // Accounts in external sign-in services, e.g. Google.
    public IReadOnlyList<UserExternalId> ExternalIds => _externalIds;

    public DateTimeOffset CreatedAt { get; private set; }

    // Invited until the user signs in for the first time (Activate, AddExternalId). Stored, so
    // never-activated users can be found in the database.
    public UserStatuses Status { get; private set; }

    // Checked at sign-in, at the authorize and token endpoints and when the sign-in cookie is validated.
    [JsonIgnore]
    public bool CanSignIn => !Locked;

    // A user created by an invitation, or on the first sign-in with a code. Invited until Activate.
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

    // A correct sign-in code proves the email: an invited user becomes Active.
    public void Activate() => Status = UserStatuses.Active;

    public void SetEmail(string email)
    {
        email = email.Trim();

        var errors = new ErrorCollector();
        ValidateEmail(errors, email);
        errors.ThrowIfAny();

        Email = email;
        NormalizedEmail = NormalizeEmail(email);
    }

    // Lock or unlock by an admin.
    public void SetLocked(bool locked) => Locked = locked;

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

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static ValidationError EmailTakenError() =>
        new(nameof(Email), ErrorCode.Taken,
            "Email is used by another user.",
            "Sähköposti on toisen käyttäjän käytössä.");
}
