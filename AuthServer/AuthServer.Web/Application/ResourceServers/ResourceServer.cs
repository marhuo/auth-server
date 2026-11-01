namespace AuthServer.Web.Application.ResourceServers;

public class ResourceServer
{
    public required Guid Id { get; init; }
    public required string Audience { get; init; } = "";
    public required string DisplayName { get; init; } = "";
    public required DateTimeOffset CreatedAt { get; init; }
    public required bool IsAuthServer { get; init; }
}
