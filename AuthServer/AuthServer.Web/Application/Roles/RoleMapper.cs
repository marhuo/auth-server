using AuthServer.Web.Persistence;
using System.Linq.Expressions;

namespace AuthServer.Web.Application.Roles;

public static class RoleMapper
{
    public static readonly Expression<Func<RoleEntity, Role>> ToDomainExpression = e => new Role
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        ResourceServerId = e.ResourceServerId,
        CreatedAt = e.CreatedAt
    };
}
