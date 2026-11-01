namespace AuthServer.Application.Clients.Messages;

public class ClientAddRedirectUriRequest
{
    public required Guid Id { get; init; }
    public required string Uri { get; init; }
}
