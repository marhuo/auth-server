namespace AuthServer.Web.Controllers.Api.Dto;

public class TenantDomainCreateRequest
{
    // E.g. "acme.fi".
    public required string Name { get; init; }
}
