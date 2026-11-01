using AuthServer.Domain.Users;

namespace AuthServer.Application.Users.Messages;

public class UserDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    // Automatic lockout after too many failed logins; ends by itself.
    public required DateTimeOffset? LockedUntil { get; init; }

    // Locked by an admin.
    public required bool Locked { get; init; }

    // Invited until the user sets a password with the invitation link.
    public required UserDtoStatuses Status { get; init; }

    // False until the invited user sets a password with the invitation link.
    public required bool HasPassword { get; init; }
    public required List<UserRoleDto> UserRoles { get; init; }

    public static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        CreatedAt = user.CreatedAt,
        LockedUntil = user.LockedUntil,
        Locked = user.Locked,
        Status = user.Status.ToDto(),
        HasPassword = user.HasPassword,
        UserRoles = user.Roles.Select(UserRoleDto.ToDto).ToList()
    };
}
