namespace AuthServer.Web.Application.Scopes;

public class Scope
{
    public required Guid Id { get; init; }
    public required string Name { get; init; } = "";
    public required string DisplayName { get; init; } = "";
    public required Guid? ResourceServerId { get; init; }
}
