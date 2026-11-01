using System.Text;
using System.Text.RegularExpressions;
using AuthServer.Web.Application.Clients;
using AuthServer.Web.Application.OAuth;
using AuthServer.Web.Application.Scopes;
using AuthServer.Web.Application.Tenants;
using AuthServer.Web.Application.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AuthServer.Web.Controllers;

public class TokenController(
    ClientService clientService,
    TenantService tenantService,
    ScopeService scopeService,
    UserService userService,
    TokenService tokenService,
    AuthorizationCodeService authorizationCodeService,
    TimeProvider timeProvider,
    IOptions<AuthTokenOptions> options) : Controller
{
    private static readonly Regex VerifierRegex = new(@"^[A-Za-z0-9\-._~]{43,128}\z", RegexOptions.Compiled);

    [HttpPost("~/connect/token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token()
    {
        var form = await Request.ReadFormAsync();

        if (form.Any(p => p.Value.Count > 1))
        {
            return OAuthError("invalid_request", "Duplicate parameters are not allowed.");
        }

        var (clientId, secret) = ReadClientCredentials(Request.Headers, form);

        var (status, client) = await clientService.AuthenticateAsync(clientId, secret);
        if (status != ClientAuthStatus.Success || client is null)
        {
            return OAuthError("invalid_client", "Client authentication failed.", 401);
        }

        var grantType = form["grant_type"].ToString();

        if (grantType is not (GrantTypes.AuthorizationCode or GrantTypes.RefreshToken or GrantTypes.ClientCredentials))
        {
            return OAuthError("unsupported_grant_type", "The grant type is not supported.");
        }

        if (!client.IsGrantTypeAllowed(grantType))
        {
            return OAuthError("unauthorized_client", "The client is not allowed to use this grant type.");
        }

        return grantType switch
        {
            GrantTypes.AuthorizationCode => await ExchangeAuthorizationCodeToTokensAsync(client, form),
            GrantTypes.RefreshToken => await ExchangeRefreshTokenToTokensAsync(client, form),
            _ => await ExchangeClientCredentialsToAccessTokenAsync(client, form)
        };
    }

    private async Task<IActionResult> ExchangeAuthorizationCodeToTokensAsync(Client client, IFormCollection form)
    {
        var authorizationCode = form["code"].ToString();
        var redirectUri = form["redirect_uri"].ToString();
        var verifier = form["code_verifier"].ToString();

        if (authorizationCode.Length == 0 || redirectUri.Length == 0 || !VerifierRegex.IsMatch(verifier))
        {
            return OAuthError("invalid_request", "code, redirect_uri and a valid code_verifier are required.");
        }

        var authorizationCodeInfo = await authorizationCodeService.UseCodeAsync(authorizationCode, client.Id, redirectUri, verifier);
        if (authorizationCodeInfo is null)
        {
            return OAuthError("invalid_grant", "The authorization code is invalid.");
        }

        var userId = authorizationCodeInfo.UserId;
        var tenantId = authorizationCodeInfo.TenantId;
        var scopes = authorizationCodeInfo.Scopes;
        var nonce = authorizationCodeInfo.Nonce;

        var user = await userService.GetByIdAsync(userId);
        if (user is null || !await IsAllowedToSignInAsync(client, user, tenantId))
        {
            return OAuthError("invalid_grant", "The user is no longer allowed to sign in.");
        }

        var refreshScopes = scopes.Contains("offline_access") && client.IsGrantTypeAllowed(GrantTypes.RefreshToken)
            ? scopes
            : null;

        return await IssueUserTokensAsync(client, user, tenantId, scopes, nonce, refreshScopes, null);
    }

    private async Task<IActionResult> ExchangeRefreshTokenToTokensAsync(Client client, IFormCollection form)
    {
        var token = form["refresh_token"].ToString();
        if (token.Length == 0)
        {
            return OAuthError("invalid_request", "refresh_token is required.");
        }

        var requested = SplitScopes(form["scope"].ToString());

        var renewedRefreshToken = await tokenService.RenewRefreshTokenAsync(token, client.Id, requested);
        if (renewedRefreshToken is null)
        {
            return OAuthError("invalid_grant", "The refresh token is invalid.");
        }

        var tenantId = renewedRefreshToken.TenantId;
        var userId = renewedRefreshToken.UserId;
        var refreshTokenScopes = renewedRefreshToken.Scopes;

        var user = await userService.GetByIdAsync(userId);
        if (user is null || !await IsAllowedToSignInAsync(client, user, tenantId))
        {
            await tokenService.RevokeFamilyAsync(renewedRefreshToken.FamilyId);
            return OAuthError("invalid_grant", "The user is no longer allowed to sign in.");
        }

        var accessTokenScopes = requested.Count > 0 ? requested : refreshTokenScopes;

        return await IssueUserTokensAsync(client, user, tenantId, accessTokenScopes, null, refreshTokenScopes, renewedRefreshToken.FamilyId);
    }

    private async Task<IActionResult> ExchangeClientCredentialsToAccessTokenAsync(Client client, IFormCollection form)
    {
        if (!client.IsConfidential)
        {
            return OAuthError("unauthorized_client", "Client credentials require a confidential client.");
        }

        var scopeNames = SplitScopes(form["scope"].ToString());
        var scopeIds = await scopeService.GetIdsByNamesAsync(scopeNames);

        if (scopeNames.Count == 0
            || scopeNames.Contains("openid")
            || scopeNames.Contains("offline_access")
            || scopeIds.Count != scopeNames.Count
            || !client.AreScopesAllowed(scopeIds))
        {
            return OAuthError("invalid_scope", "One or more scopes are not allowed.");
        }

        var response = new Dictionary<string, object>
        {
            ["access_token"] = await tokenService.CreateAccessTokenAsync(client.ClientId, null, client.TenantId, scopeNames),
            ["token_type"] = "Bearer",
            ["expires_in"] = (int)options.Value.AccessTokenLifetime.TotalSeconds,
            ["scope"] = string.Join(' ', scopeNames)
        };

        return TokenResponse(response);
    }

    private async Task<IActionResult> IssueUserTokensAsync(
        Client client,
        User user,
        Guid tenantId,
        IReadOnlyCollection<string> scopes,
        string? nonce,
        IReadOnlyCollection<string>? refreshScopes,
        Guid? familyId)
    {
        var response = new Dictionary<string, object>
        {
            ["access_token"] = await tokenService.CreateAccessTokenAsync(client.ClientId, user.Id, tenantId, scopes),
            ["token_type"] = "Bearer",
            ["expires_in"] = (int)options.Value.AccessTokenLifetime.TotalSeconds,
            ["scope"] = string.Join(' ', scopes)
        };

        if (scopes.Contains("openid"))
        {
            response["id_token"] = await tokenService.CreateIdTokenAsync(client, user, tenantId, scopes, nonce);
        }

        if (refreshScopes is not null)
        {
            response["refresh_token"] = await tokenService.CreateRefreshTokenAsync(client.Id, user.Id, tenantId, refreshScopes, familyId);
        }

        return TokenResponse(response);
    }

    private async Task<bool> IsAllowedToSignInAsync(Client client, User user, Guid tenantId)
    {
        if (user.IsLocked(timeProvider.GetUtcNow()))
            return false;

        if (client.TenantId is not null && client.TenantId != tenantId)
            return false;

        var tenantIds = await tenantService.GetUserTenantIdsAsync(user.Id, client.ResourceServerId, timeProvider.GetUtcNow());
        return tenantIds.Contains(tenantId);
    }

    private static (string? ClientId, string? Secret) ReadClientCredentials(IHeaderDictionary headers, IFormCollection form)
    {
        var authorization = headers.Authorization.ToString();

        if (authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return ReadBasicCredentials(authorization["Basic ".Length..].Trim());
        }

        return (form["client_id"].ToString(), form["client_secret"].ToString());
    }

    private static (string? ClientId, string? Secret) ReadBasicCredentials(string base64)
    {
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var separator = decoded.IndexOf(':');

            if (separator > 0)
            {
                return (
                    System.Net.WebUtility.UrlDecode(decoded[..separator]),
                    System.Net.WebUtility.UrlDecode(decoded[(separator + 1)..]));
            }
        }
        catch (FormatException)
        {
        }

        return (null, null);
    }

    private static List<string> SplitScopes(string value) =>
        value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();

    private IActionResult TokenResponse(Dictionary<string, object> body)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        return Json(body);
    }

    private IActionResult OAuthError(string error, string description, int status = 400)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";

        if (status == 401)
        {
            Response.Headers.WWWAuthenticate = "Basic realm=\"AuthServer\"";
        }

        return new JsonResult(new Dictionary<string, string>
        {
            ["error"] = error,
            ["error_description"] = description
        })
        {
            StatusCode = status
        };
    }
}