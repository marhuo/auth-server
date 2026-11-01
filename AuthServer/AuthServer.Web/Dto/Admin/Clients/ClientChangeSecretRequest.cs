namespace AuthServer.Web.Dto.Admin.Clients;

public class ClientChangeSecretRequest
{
    public Guid Id { get; set; }
    public string NewSecret { get; set; } = "";
}
