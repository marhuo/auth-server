namespace AuthServer.Web.Controllers.Api.Dto;

public class TenantDomainDto
{
    public required Guid Id { get; init; }

    // E.g. "acme.fi".
    public required string Name { get; init; }

    // The TXT record the tenant adds to the domain's DNS.
    public required string TxtRecord { get; init; }

    public required DateTimeOffset? VerifiedAt { get; init; }
}
