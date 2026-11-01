namespace AuthServer.Web.Application.Clients;

public static class ClientGrants
{
    public static IReadOnlyList<string> For(ClientTypes type) => type switch
    {
        ClientTypes.PublicApplication => [GrantTypes.AuthorizationCode, GrantTypes.RefreshToken],
        ClientTypes.ConfidentialMachine => [GrantTypes.ClientCredentials],
        _ => []
    };
}