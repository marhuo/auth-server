namespace AuthServer.Web.Dto.Admin.Tenants;

public class TenantUpdateRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
}
