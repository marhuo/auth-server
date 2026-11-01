namespace AuthServer.Web.Dto.Admin.Clients;

public class ClientRemoveScopeRequest
{
    public Guid Id { get; init; }
    public Guid ScopeId { get; init; }
}

