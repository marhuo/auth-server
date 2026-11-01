namespace AuthServer.Web.Dto.Admin.Tenants;

public class TenantSuspendRequest
{
    public Guid Id { get; set; }
    public string SuspensionReason { get; set; } = "";
}
