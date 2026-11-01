using AuthServer.Web.Persistence;
using System.Linq.Expressions;

namespace AuthServer.Web.Application.Tenants;

public static class TenantMapper
{
    public static readonly Expression<Func<TenantEntity, Tenant>> ToDomainExpression = e => new Tenant
    {
        Id = e.Id,
        Name = e.Name,
        Slug = e.Slug,
        SuspendedAt = e.SuspendedAt,
        SuspensionReason = e.SuspensionReason,
        CreatedAt = e.CreatedAt
    };
}
