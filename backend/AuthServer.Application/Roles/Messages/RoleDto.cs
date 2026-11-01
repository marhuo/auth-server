using AuthServer.Domain.Roles;

namespace AuthServer.Application.Roles.Messages;

public class RoleDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required Guid ResourceServerId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public static RoleDto ToDto(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        ResourceServerId = role.ResourceServerId,
        CreatedAt = role.CreatedAt
    };
}
