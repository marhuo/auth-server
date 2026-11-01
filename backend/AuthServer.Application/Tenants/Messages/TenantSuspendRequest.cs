namespace AuthServer.Application.Tenants.Messages;

public class TenantSuspendRequest
{
    public required Guid Id { get; init; }
    public required string SuspensionReason { get; init; }
}
