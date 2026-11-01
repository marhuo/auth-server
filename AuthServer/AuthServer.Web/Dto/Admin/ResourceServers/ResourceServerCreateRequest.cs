namespace AuthServer.Web.Dto.Admin.ResourceServers;

public class ResourceServerCreateRequest
{
    public string Audience { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
