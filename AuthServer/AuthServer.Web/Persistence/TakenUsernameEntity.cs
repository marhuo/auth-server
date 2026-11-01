namespace AuthServer.Web.Persistence;

public class TakenUsernameEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string NormalizedUsername { get; set; } = "";
    public virtual UserEntity User { get; set; } = null!;
}
