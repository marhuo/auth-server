namespace AuthServer.Infrastructure.Persistence.Ef;

public class UserExternalIdEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = "";
    public string ProviderUserId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
