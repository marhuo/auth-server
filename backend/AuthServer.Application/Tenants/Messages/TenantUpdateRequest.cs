namespace AuthServer.Application.Tenants.Messages;

public class TenantUpdateRequest
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
}
