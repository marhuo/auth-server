using AuthServer.Domain.Users;

namespace AuthServer.Application.Users.Messages;

public class UserRoleDto
{
    public required Guid RoleId { get; init; }
    public required Guid? TenantId { get; init; }

    public static UserRoleDto ToDto(UserRole role) => new()
    {
        RoleId = role.RoleId,
        TenantId = role.TenantId,
    };
}
