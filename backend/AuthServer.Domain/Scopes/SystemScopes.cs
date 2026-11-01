namespace AuthServer.Domain.Scopes;

// Names of the built-in scopes of the auth server itself.
public static class SystemScopes
{
    // Lets a client call the auth server's external API (Controllers/Api): create tenants, invite users.
    public const string AuthServerApi = "auth_server:api";
    public const string AuthServerAdmin = "auth-server.admin";
}
