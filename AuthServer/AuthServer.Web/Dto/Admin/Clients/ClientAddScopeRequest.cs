namespace AuthServer.Web.Dto.Admin.Clients;

public class ClientAddScopeRequest
{
    public Guid Id { get; init; }
    public Guid ScopeId { get; init; }
}
