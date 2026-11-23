namespace AuthServer.Application.Tenants.Messages;

public class TenantDomainCreateRequest
{
    public required Guid TenantId { get; init; }

    // E.g. "acme.fi".
    public required string Name { get; init; }
}
