using AuthServer.Domain.Users;

namespace AuthServer.Application.Users.Messages;

// See UserStatuses.
public enum UserDtoStatuses
{
    Invited,
    Active
}

public static class UserDtoStatusesExtensions
{
    extension(UserStatuses status)
    {
        public UserDtoStatuses ToDto() => status switch
        {
            UserStatuses.Invited => UserDtoStatuses.Invited,
            UserStatuses.Active => UserDtoStatuses.Active,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
}
