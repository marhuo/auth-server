
namespace AuthServer.Domain.Clients;

public static class ClientGrants
{
    public static IReadOnlyList<string> For(ClientTypes type) => type switch
    {
        ClientTypes.PublicApplication => [GrantTypes.AuthorizationCode, GrantTypes.RefreshToken],
        ClientTypes.ConfidentialApplication => [GrantTypes.AuthorizationCode, GrantTypes.RefreshToken],
        ClientTypes.ConfidentialMachine => [GrantTypes.ClientCredentials],
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
