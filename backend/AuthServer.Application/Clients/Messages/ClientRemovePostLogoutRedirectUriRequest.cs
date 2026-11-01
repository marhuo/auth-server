namespace AuthServer.Application.Clients.Messages;

public class ClientRemovePostLogoutRedirectUriRequest
{
    public required Guid Id { get; init; }
    public required string Uri { get; init; }
}
