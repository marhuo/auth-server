namespace AuthServer.Domain.Common;

public abstract class AggregateRoot
{
    protected AggregateRoot(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; private set; }
}
