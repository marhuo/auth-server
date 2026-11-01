namespace AuthServer.Application.ResourceServers.Messages;

public class ResourceServerCreateRequest
{
    public required string Audience { get; init; }
    public required string DisplayName { get; init; }
}
