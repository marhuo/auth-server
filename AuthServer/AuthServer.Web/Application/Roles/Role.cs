namespace AuthServer.Web.Application.Roles;

public class Role
{
    public required Guid Id { get; init; }
    public required string Name { get; init; } = "";
    public required string Description { get; init; } = "";
    public required Guid ResourceServerId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
