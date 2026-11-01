namespace AuthServer.Application.Scopes.Messages;

public class ScopeCreateRequest
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }

    // Required; nullable so that a missing value is reported by ScopeValidator.
    public Guid? ResourceServerId { get; init; }
}
