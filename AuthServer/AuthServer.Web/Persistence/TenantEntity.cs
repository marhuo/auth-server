namespace AuthServer.Web.Persistence;

public class TenantEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    /// <summary>
    /// Short url friendly name
    /// </summary>
    public string Slug { get; set; } = "";
    public DateTimeOffset? SuspendedAt { get; set; }
    public string? SuspensionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public virtual List<UserRoleEntity> UserRoles { get; set; } = [];
    public virtual List<ClientEntity> Clients { get; set; } = [];
    public virtual List<SubscriptionEntity> Subscriptions { get; set; } = [];

}
