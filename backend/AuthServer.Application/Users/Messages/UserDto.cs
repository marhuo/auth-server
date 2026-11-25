using AuthServer.Domain.Users;

namespace AuthServer.Application.Users.Messages;

public class UserDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    // Locked by an admin.
    public required bool Locked { get; init; }

    // Invited until the user signs in for the first time.
    public required UserDtoStatuses Status { get; init; }

    public required List<UserRoleDto> UserRoles { get; init; }

    public static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        CreatedAt = user.CreatedAt,
        Locked = user.Locked,
        Status = user.Status.ToDto(),
        UserRoles = user.Roles.Select(UserRoleDto.ToDto).ToList()
    };
}
