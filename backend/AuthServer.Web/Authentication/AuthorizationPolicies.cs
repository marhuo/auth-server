namespace AuthServer.Web.Authentication;

// Authorization policy names, named after what they protect. Policies are defined in Program.cs.
public static class AuthorizationPolicies
{
    // Admin UI, admin API (Controllers/Admin), log viewer and Hangfire dashboard.
    // Requires the SystemRoles.AuthServerAdmin role (sign-in cookie).
    public const string Admin = "Admin";

    // External integration API (Controllers/Api).
    // Requires a bearer token with the auth_server:api scope.
    public const string ExternalApi = "ExternalApi";
}
