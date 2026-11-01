using AuthServer.Web.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Web.Application.Subscriptions;

public class SubscriptionService(AuthDbContext db)
{

    public async Task<(bool Ok, string? Error, Guid? id)> CreateAsync(Guid tenantId, Guid planId, SubscriptionPeriodKind subscriptionPeriodKind, DateTimeOffset start)
    {
        var plan = await db.Plans.SingleOrDefaultAsync(p => p.Id == planId);
        if (plan == null)
            return (false, "Plan does not exist", null);

        if(!await db.Tenants.AnyAsync(t => t.Id == tenantId))
            return (false, "Tenant does not exist", null);

        var end = plan.Duration.GetEndDate(start);
        var overlaps = await db.Subscriptions.AnyAsync(s => s.TenantId == tenantId && s.ResourceServerId == plan.ResourceServerId && (s.EndsAt == null || s.EndsAt > start) &&  (end == null || s.StartsAt < end));
        
        if (await db.Subscriptions.AnyAsync(s => s.TenantId == tenantId && s.ResourceServerId == plan.ResourceServerId && (s.EndsAt == null || s.EndsAt >= start)))
        {
            return (false, "Subscription already exists", null);
        }

        var id = Guid.CreateVersion7();
        db.Subscriptions.Add(new SubscriptionEntity
        {
            Id = id,
            TenantId = tenantId,
            PlanId = plan.Id,
            ResourceServerId = plan.ResourceServerId,
            CreatedAt = DateTimeOffset.UtcNow,
            StartsAt = start,
            EndsAt = end,
            Kind = subscriptionPeriodKind,
            MaxUsers = plan.MaxUsers
        });

        await db.SaveChangesAsync();
        return (true, null, id);
    }

    public Task<bool> HasAccessAsync(Guid tenantId, Guid resourceServerId)
    {
        var now = DateTimeOffset.UtcNow;

        return db.Subscriptions.AnyAsync(s =>
            s.TenantId == tenantId
            && s.ResourceServerId == resourceServerId
            && s.StartsAt <= now
            && (s.EndsAt == null || s.EndsAt > now)
            && s.Tenant.SuspendedAt == null);
    }
}
