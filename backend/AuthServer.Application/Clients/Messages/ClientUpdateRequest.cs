namespace AuthServer.Application.Clients.Messages;

public class ClientUpdateRequest
{
    public required Guid Id { get; init; }
    public required string ClientId { get; init; }
    public required string DisplayName { get; init; }
    public Guid? TenantId { get; init; }

    // See Client.InitiateLoginUri. Null or blank clears it.
    public string? InitiateLoginUri { get; init; }
}
