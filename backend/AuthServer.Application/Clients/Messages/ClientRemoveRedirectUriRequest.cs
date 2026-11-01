namespace AuthServer.Application.Clients.Messages;

public class ClientRemoveRedirectUriRequest
{
    public required Guid Id { get; init; }
    public required string Uri { get; init; }
}
