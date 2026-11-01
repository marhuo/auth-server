namespace AuthServer.Application.ResourceServers.Messages;

public class ResourceServerCreateRequest
{
    public required string Audience { get; init; }
    public required string DisplayName { get; init; }

    // Secret of the client that is created for the resource server for calling the auth server's
    // external API (ResourceServer.BackendClientId).
    public required string Secret { get; init; }
}
