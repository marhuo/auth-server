namespace AuthServer.Application.Scopes.Messages;

public class ScopeUpdateRequest
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string DisplayName { get; init; }

    // Null only for global (standard) scopes.
    public Guid? ResourceServerId { get; init; }
}
