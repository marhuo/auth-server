namespace AuthServer.Web.Dto.Admin.Clients;

public class ClientRemoveRedirectUriRequest
{
    public Guid Id { get; set; }
    public string Uri { get; set; } = "";
}
