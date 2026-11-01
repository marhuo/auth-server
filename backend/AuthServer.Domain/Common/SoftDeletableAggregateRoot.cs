using System.Text.Json.Serialization;

namespace AuthServer.Domain.Common;

public abstract class SoftDeletableAggregateRoot : AggregateRoot
{
    protected SoftDeletableAggregateRoot(Guid id, DateTimeOffset? deletedAt) : base(id)
    {
        DeletedAt = deletedAt;
    }

    public DateTimeOffset? DeletedAt { get; private set; }

    [JsonIgnore]
    public bool Deleted => DeletedAt is not null;

    public virtual void Delete(DateTimeOffset now)
    {
        EnsureNotDeleted();
        DeletedAt = now;
    }

    protected void EnsureNotDeleted()
    {
        if (Deleted)
            throw new InvalidOperationException($"{GetType().Name} {Id} is deleted.");
    }
}
