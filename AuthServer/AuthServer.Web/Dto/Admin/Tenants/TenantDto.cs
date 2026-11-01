namespace AuthServer.Web.Dto.Admin.Tenants;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public DateTimeOffset? SuspendedAt { get; set; }
    public string? SuspensionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
