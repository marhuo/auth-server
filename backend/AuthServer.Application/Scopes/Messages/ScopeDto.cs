using AuthServer.Domain.Scopes;

namespace AuthServer.Application.Scopes.Messages;

public class ScopeDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required Guid? ResourceServerId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required bool IsStandard { get; init; }

    public static ScopeDto ToDto(Scope scope) => new()
    {
        Id = scope.Id,
        Name = scope.Name,
        DisplayName = scope.DisplayName,
        ResourceServerId = scope.ResourceServerId,
        CreatedAt = scope.CreatedAt,
        IsStandard = scope.IsStandard
    };
}
