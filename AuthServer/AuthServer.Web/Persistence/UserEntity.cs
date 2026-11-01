namespace AuthServer.Web.Persistence;

public class UserEntity
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public string NormalizedUserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public string? PasswordHash { get; set; }
    public int FailedLogins { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public virtual List<UserRoleEntity> UserRoles { get; set; } = [];
    public virtual List<TakenUsernameEntity> TakenUsernames { get; set; } = [];
}

