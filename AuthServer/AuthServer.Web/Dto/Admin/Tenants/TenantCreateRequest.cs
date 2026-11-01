namespace AuthServer.Web.Dto.Admin.Tenants;

public class TenantCreateRequest
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
}
