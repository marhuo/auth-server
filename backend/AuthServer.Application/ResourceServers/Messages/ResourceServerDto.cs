using AuthServer.Domain.ResourceServers;

namespace AuthServer.Application.ResourceServers.Messages;

public class ResourceServerDto
{
    public required Guid Id { get; init; }
    public required string Audience { get; init; }
    public required string DisplayName { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required bool IsAuthServer { get; init; }

    public static ResourceServerDto ToDto(ResourceServer resourceServer) => new()
    {
        Id = resourceServer.Id,
        Audience = resourceServer.Audience,
        DisplayName = resourceServer.DisplayName,
        CreatedAt = resourceServer.CreatedAt,
        IsAuthServer = resourceServer.IsAuthServer
    };
}
