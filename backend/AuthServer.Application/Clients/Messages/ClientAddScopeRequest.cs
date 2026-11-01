namespace AuthServer.Application.Clients.Messages;

public class ClientAddScopeRequest
{
    public required Guid Id { get; init; }
    public required Guid ScopeId { get; init; }
}
