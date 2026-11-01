namespace AuthServer.Web.Application.Users;

public class UserIdentity
{
    public required Guid Id { get; init; }
    public required string UserName { get; init; } = "";
    public required string Email { get; init; } = "";
}