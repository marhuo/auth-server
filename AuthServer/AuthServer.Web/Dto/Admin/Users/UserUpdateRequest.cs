namespace AuthServer.Web.Dto.Admin.Users;

public class UserUpdateRequest
{
    public required Guid Id { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public DateTimeOffset? LockedUntil { get; init; }
}
