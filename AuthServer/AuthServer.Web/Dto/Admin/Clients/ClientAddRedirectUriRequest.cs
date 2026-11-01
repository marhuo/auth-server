namespace AuthServer.Web.Dto.Admin.Clients;

public class ClientAddRedirectUriRequest
{
    public Guid Id { get; set; }
    public string Uri { get; set; } = "";
}
