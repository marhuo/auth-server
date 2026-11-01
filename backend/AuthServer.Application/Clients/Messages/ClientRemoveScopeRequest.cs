namespace AuthServer.Application.Clients.Messages;

public class ClientRemoveScopeRequest
{
    public required Guid Id { get; init; }
    public required Guid ScopeId { get; init; }
}
