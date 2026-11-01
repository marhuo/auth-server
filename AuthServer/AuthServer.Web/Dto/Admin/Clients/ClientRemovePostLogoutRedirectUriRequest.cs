namespace AuthServer.Web.Dto.Admin.Clients;

public class ClientRemovePostLogoutRedirectUriRequest
{
    public Guid Id { get; set; }
    public string Uri { get; set; } = "";
}
