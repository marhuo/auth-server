namespace AuthServer.Application.ResourceServers.Messages;

public class ResourceServerUpdateRequest
{
    public required Guid Id { get; init; }
    public required string Audience { get; init; }
    public required string DisplayName { get; init; }
}
