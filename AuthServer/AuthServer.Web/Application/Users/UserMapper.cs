using AuthServer.Web.Persistence;
using System.Linq.Expressions;

namespace AuthServer.Web.Application.Users;

public static class UserMapper
{
    public static readonly Expression<Func<UserEntity, User>> ToDomainExpression = e => new User
    {
        Id = e.Id,
        UserName = e.UserName,
        Email = e.Email,
        LockedUntil = e.LockedUntil,
        CreatedAt = e.CreatedAt,
        HasPassword = e.PasswordHash != null,
        UserRoles = e.UserRoles.Select(ur => new UserRole
        {
            RoleId = ur.RoleId,
            TenantId = ur.TenantId,
            CreatedAt = ur.CreatedAt
        }).ToList()
    };

}
