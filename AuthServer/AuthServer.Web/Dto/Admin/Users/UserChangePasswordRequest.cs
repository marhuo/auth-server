namespace AuthServer.Web.Dto.Admin.Users;

public class UserChangePasswordRequest
{
    public required Guid Id { get; init; }
    public required string NewPassword { get; init; }
}