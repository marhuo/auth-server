namespace AuthServer.Application.Users.Messages;

public class UserUpdateRequest
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }

    // Locked by an admin. Setting false also ends the automatic lockout (LockedUntil).
    public required bool Locked { get; init; }
}
