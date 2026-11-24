using AuthServer.Domain.Tenants;

namespace AuthServer.Application.Tenants.Messages;

public class TenantDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required DateTimeOffset? SuspendedAt { get; init; }
    public required string? SuspensionReason { get; init; }

    // Null: nobody has signed in to the tenant, so it can be deleted.
    public required DateTimeOffset? FirstSignInAt { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public static TenantDto ToDto(Tenant tenant) => new()
    {
        Id = tenant.Id,
        Name = tenant.Name,
        Slug = tenant.Slug,
        SuspendedAt = tenant.SuspendedAt,
        SuspensionReason = tenant.SuspensionReason,
        FirstSignInAt = tenant.FirstSignInAt,
        CreatedAt = tenant.CreatedAt
    };
}
