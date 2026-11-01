namespace AuthServer.Application.Users.Messages;

public class UserChangePasswordRequest
{
    public required Guid Id { get; init; }
    public required string NewPassword { get; init; }
}
