namespace AuthServer.Application.Roles.Messages;

public class RoleCreateRequest
{
    public required Guid ResourceServerId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
}
