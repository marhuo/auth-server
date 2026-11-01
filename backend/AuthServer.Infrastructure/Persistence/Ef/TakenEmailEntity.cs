namespace AuthServer.Infrastructure.Persistence.Ef;

// Every normalized email a user has had. The email stays reserved for that user after the user
// changes it, so an email always identifies one user (also in logs).
public class TakenEmailEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string NormalizedEmail { get; set; } = "";
}
