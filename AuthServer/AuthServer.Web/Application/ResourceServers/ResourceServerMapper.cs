using AuthServer.Web.Persistence;
using System.Linq.Expressions;

namespace AuthServer.Web.Application.ResourceServers;

public static class ResourceServerMapper
{
    public static readonly Expression<Func<ResourceServerEntity, ResourceServer>> ToDomainExpression =
       e => new ResourceServer
       {
           Id = e.Id,
           Audience = e.Audience,
           DisplayName = e.DisplayName,
           CreatedAt = e.CreatedAt,
           IsAuthServer = e.IsAuthServer
       };

}
