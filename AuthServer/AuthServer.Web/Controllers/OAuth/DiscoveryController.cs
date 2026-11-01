using AuthServer.Web.Application.Clients;
using AuthServer.Web.Application.OAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AuthServer.Web.Controllers.OAuth;

[ApiController]
public class DiscoveryController(IOptions<AuthTokenOptions> options) : ControllerBase
{
    [HttpGet("~/.well-known/openid-configuration")]
    public IActionResult Configuration()
    {
        var issuer = options.Value.Issuer.TrimEnd('/');

        Response.Headers.CacheControl = "public, max-age=300";

        return Ok(new Dictionary<string, object>
        {
            ["issuer"] = options.Value.Issuer,
            ["authorization_endpoint"] = $"{issuer}/connect/authorize",
            ["token_endpoint"] = $"{issuer}/connect/token",
            ["jwks_uri"] = $"{issuer}/.well-known/jwks.json",
            ["response_types_supported"] = new[] { "code" },
            ["grant_types_supported"] = new[]
            {
                GrantTypes.AuthorizationCode,
                GrantTypes.RefreshToken,
                GrantTypes.ClientCredentials
            },
            ["subject_types_supported"] = new[] { "public" },
            ["id_token_signing_alg_values_supported"] = new[] { "RS256" },
            ["code_challenge_methods_supported"] = new[] { "S256" },
            ["token_endpoint_auth_methods_supported"] = new[] { "client_secret_basic", "client_secret_post", "none" },
            ["scopes_supported"] = new[] { "openid", "profile", "email", "offline_access" },
            ["claims_supported"] = new[] { "sub", "name", "email", "tenant_id", "role" },
            ["end_session_endpoint"] = $"{issuer}/connect/logout",
            ["userinfo_endpoint"] = $"{issuer}/connect/userinfo",
        });
    }
}