namespace AuthServer.Infrastructure.Persistence.Ef;

// Entities implementing this get a global query filter (deleted_at IS NULL) in AuthDbContext.
public interface ISoftDeletableEntity
{
    DateTimeOffset? DeletedAt { get; }
}
