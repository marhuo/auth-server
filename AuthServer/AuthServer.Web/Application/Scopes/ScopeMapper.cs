using AuthServer.Web.Persistence;
using System.Linq.Expressions;

namespace AuthServer.Web.Application.Scopes;

public static class ScopeMapper
{
    public static readonly Expression<Func<ScopeEntity, Scope>> ToDomainExpression = e => new Scope
    {
        Id = e.Id,
        Name = e.Name,
        DisplayName = e.DisplayName,
        ResourceServerId = e.ResourceServerId,
        CreatedAt = e.CreatedAt,
        IsStandard = e.IsStandard
    };
}
