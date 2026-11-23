using AuthServer.Domain.Tenants;

namespace AuthServer.Application.Tenants.Messages;

public class TenantDomainDto
{
    public required Guid Id { get; init; }
    public required Guid TenantId { get; init; }
    public required string Name { get; init; }

    // The TXT record the tenant adds to the domain's DNS.
    public required string TxtRecord { get; init; }

    public required DateTimeOffset? VerifiedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public static TenantDomainDto ToDto(TenantDomain domain) => new()
    {
        Id = domain.Id,
        TenantId = domain.TenantId,
        Name = domain.Name,
        TxtRecord = domain.TxtRecord,
        VerifiedAt = domain.VerifiedAt,
        CreatedAt = domain.CreatedAt
    };
}
