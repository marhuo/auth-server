namespace AuthServer.Web.Persistence;

public class UserTokenEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Purpose { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}