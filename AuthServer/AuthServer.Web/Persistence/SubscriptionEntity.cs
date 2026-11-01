namespace AuthServer.Web.Persistence;

public enum SubscriptionPeriodKind { Trial, Paid, Grace }
public class SubscriptionEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public virtual TenantEntity Tenant { get; set; } = null!;
    public Guid PlanId { get; set; }
    public Guid ResourceServerId { get; set; }
    public virtual ResourceServerEntity ResourceServer { get; set; } = null!;
    public virtual PlanEntity Plan { get; set; } = null!;
    public SubscriptionPeriodKind Kind { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public string? ExternalEventId { get; set; }
    public string? Note { get; set; }
    public int? MaxUsers { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
