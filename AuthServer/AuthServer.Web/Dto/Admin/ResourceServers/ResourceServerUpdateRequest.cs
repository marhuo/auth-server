namespace AuthServer.Web.Dto.Admin.ResourceServers;

public class ResourceServerUpdateRequest
{
    public Guid Id { get; set; }
    public string Audience { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
