namespace AuthServer.Application.Roles.Messages;

public class RoleUpdateRequest
{
    public required Guid Id { get; init; }
    public required Guid ResourceServerId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
}
