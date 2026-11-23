using AuthServer.Domain.Users;

namespace AuthServer.Infrastructure.Persistence.Ef;

public class UserEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public string? PasswordHash { get; set; }
    public int FailedLogins { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public bool Locked { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public virtual List<UserRoleEntity> UserRoles { get; set; } = [];

    // Roles are not set here; the repository syncs them.
    public static UserEntity FromDomain(User user)
    {
        var entity = new UserEntity { Id = user.Id, CreatedAt = user.CreatedAt };
        entity.UpdateFrom(user);
        return entity;
    }

    // Roles are not set here; the repository syncs them.
    public void UpdateFrom(User user)
    {
        Email = user.Email;
        NormalizedEmail = user.NormalizedEmail;
        PasswordHash = user.PasswordHash;
        FailedLogins = user.FailedLogins;
        LockedUntil = user.LockedUntil;
        Locked = user.Locked;
    }

    // UserRoles must be loaded.
    public User ToDomain() => new(
        id: Id,
        email: Email,
        normalizedEmail: NormalizedEmail,
        passwordHash: PasswordHash,
        failedLogins: FailedLogins,
        lockedUntil: LockedUntil,
        locked: Locked,
        roles: UserRoles
            .Select(ur => new UserRole(RoleId: ur.RoleId, TenantId: ur.TenantId, CreatedAt: ur.CreatedAt))
            .ToList(),
        createdAt: CreatedAt);
}
