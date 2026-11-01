namespace AuthServer.Web.Application.Tenants;

public class Tenant
{
    public required Guid Id { get; init; }
    public required string Name { get; init; } = "";
    public required string Slug { get; init; } = "";
    public required DateTimeOffset? SuspendedAt { get; init; }
    public required string? SuspensionReason { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
