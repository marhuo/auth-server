namespace AuthServer.Application.Users.Messages;

public class UserUpdateRequest
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }

    // Locked by an admin.
    public required bool Locked { get; init; }
}
