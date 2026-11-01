namespace AuthServer.Web.Dto.Admin.Clients;

public class ClientAddPostLogoutRedirectUriRequest
{
    public Guid Id { get; set; }
    public string Uri { get; set; } = "";
}
