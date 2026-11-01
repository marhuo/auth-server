//AuthorizeController.cs
using AuthServer.Domain.Clients;
using AuthServer.Application.OAuth.Services;
using AuthServer.Application.Scopes.Services;
using AuthServer.Application.Tenants.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace AuthServer.Web.Controllers.OAuth;

public class AuthorizeController(
    ClientAuthenticationService clientService,
    TenantService tenantService,
    AuthorizationCodeService authorizationCodeService,
    ScopeService scopeService,
    UserAuthenticationService userService,
    TimeProvider timeProvider) : Controller
{
    private static readonly Regex CodeChallengeRegex = new(@"^[A-Za-z0-9_-]{43}\z", RegexOptions.Compiled);

    [HttpGet("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        if (Request.Query.Any(p => p.Value.Count > 1))
        {
            return BadRequest("Duplicate parameters are not allowed.");
        }

        var clientId = Request.Query["client_id"].ToString();
        var redirectUri = Request.Query["redirect_uri"].ToString();
        var state = Request.Query["state"].ToString();

        var client = await clientService.FindActiveAsync(clientId);
        if (client is null || !client.IsRedirectUriAllowed(redirectUri))
        {
            return BadRequest("Invalid client_id or redirect_uri.");
        }

        if (Request.Query["response_type"] != "code")
        {
            return RedirectError(redirectUri, state, "unsupported_response_type", "Only response_type=code is supported.");
        }

        if (!client.IsGrantTypeAllowed(GrantTypes.AuthorizationCode))
        {
            return RedirectError(redirectUri, state, "unauthorized_client", "The client is not allowed to use the authorization code flow.");
        }

        var codeChallenge = Request.Query["code_challenge"].ToString();
        if (Request.Query["code_challenge_method"] != "S256" || !CodeChallengeRegex.IsMatch(codeChallenge))
        {
            return RedirectError(redirectUri, state, "invalid_request", "A valid S256 code_challenge is required.");
        }

        var scopeNames = Request.Query["scope"].ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToList();

        var scopeIds = await scopeService.GetIdsByNamesAsync(scopeNames);

        if (!client.AreScopesAllowed(scopeIds) || scopeIds.Count != scopeNames.Count)
        {
            return RedirectError(redirectUri, state, "invalid_scope", "One or more scopes are not allowed for this client.");
        }

        var authentication = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authentication.Succeeded)
        {
            return RedirectToPage("/Login", new { returnUrl = Request.Path + Request.QueryString });
        }

        // The sign-in cookie is validated only periodically (UserSessionValidator), so check here too
        // that a locked user does not get an authorization code.
        if (!Guid.TryParse(authentication.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || await userService.GetByIdAsync(userId) is not { } user
            || !user.CanSignIn(timeProvider.GetUtcNow()))
        {
            return RedirectError(redirectUri, state, "access_denied", "The user is not allowed to sign in.");
        }

        var tenantIds = await tenantService.GetUserTenantIdsAsync(userId, client.ResourceServerId);
        if (client.TenantId is { } clientTenantId)
        {
            tenantIds = tenantIds.Where(t => t == clientTenantId).ToList();
        }

        var requestedTenant = Request.Query["tenant"].ToString();
        Guid tenantId;

        if (!string.IsNullOrEmpty(requestedTenant))
        {
            if (!Guid.TryParse(requestedTenant, out tenantId) || !tenantIds.Contains(tenantId))
            {
                return RedirectError(redirectUri, state, "access_denied", "The user has no access to the requested tenant.");
            }
        }
        else if (tenantIds.Count == 1)
        {
            tenantId = tenantIds[0];
        }
        else if (tenantIds.Count == 0)
        {
            return RedirectError(redirectUri, state, "access_denied", "The user has no access to any tenant.");
        }
        else
        {
            return RedirectToPage("/SelectTenant", new { returnUrl = Request.Path + Request.QueryString });
        }

        var nonce = Request.Query["nonce"].ToString();

        var code = await authorizationCodeService.CreateAsync(
            client.Id,
            userId,
            tenantId,
            redirectUri,
            scopeNames,
            codeChallenge,
            string.IsNullOrEmpty(nonce) ? null : nonce);

        var parameters = new Dictionary<string, string?> { ["code"] = code };
        if (!string.IsNullOrEmpty(state))
        {
            parameters["state"] = state;
        }

        return Redirect(QueryHelpers.AddQueryString(redirectUri, parameters));
    }

    private IActionResult RedirectError(string redirectUri, string state, string error, string description)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["error"] = error,
            ["error_description"] = description
        };

        if (!string.IsNullOrEmpty(state))
        {
            parameters["state"] = state;
        }

        return Redirect(QueryHelpers.AddQueryString(redirectUri, parameters));
    }
}
