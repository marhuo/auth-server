namespace AuthServer.Web.Application.Users;

public class User
{
    public required Guid Id { get; init; }
    public required string UserName { get; init; } = "";
    public required string Email { get; init; } = "";
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? LockedUntil { get; init; }
    public required List<UserRole> UserRoles { get; init; } = [];
    public required bool HasPassword { get; init; }
    public bool IsLocked(DateTimeOffset now) => LockedUntil > now;
}
