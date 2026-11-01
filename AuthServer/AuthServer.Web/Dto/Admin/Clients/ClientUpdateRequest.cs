namespace AuthServer.Web.Dto.Admin.Clients
{
    public class ClientUpdateRequest
    {
        public Guid Id { get; set; }
        public string ClientId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public Guid? TenantId { get; set; }
    }
}
