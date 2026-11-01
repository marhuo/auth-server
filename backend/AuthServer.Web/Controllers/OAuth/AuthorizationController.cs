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
        // CSRF protection for the client: the client sends a random value and checks that the same
        // value comes back. The auth server only returns it unchanged.
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
            // prompt=create (OpenID Connect): the product asks for the registration page instead of the
            // login page, e.g. from its "Try it free" button. After registering, the user comes back here.
            var page = Request.Query["prompt"] == "create" ? "/Register" : "/Login";
            return RedirectToPage(page, new { returnUrl = Request.Path + Request.QueryString });
        }

        // The sign-in cookie is validated only periodically (UserSessionValidator), so check here too
        // that a locked user does not get an authorization code.
        var user = Guid.TryParse(authentication.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? await userService.GetByIdAsync(userId)
            : null;

        // The cookie points to a user that does not exist (e.g. the database was recreated in
        // development): sign out and show the login page instead of an error.
        if (user is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Login", new { returnUrl = Request.Path + Request.QueryString });
        }

        if (!user.CanSignIn(timeProvider.GetUtcNow()))
        {
            return RedirectError(redirectUri, state, "access_denied", "The user is not allowed to sign in.");
        }

        // Which organization (tenant) the tokens are for.
        var userTenantIds = await GetUserTenantIdsAsync(userId, client);
        var chosenTenantId = GetChosenTenantId();
        Guid? tenantId;

        // 1. The user chose an organization on the selection page: use it, if the user belongs to it.
        if (chosenTenantId is not null)
        {
            if (!userTenantIds.Contains(chosenTenantId.Value))
            {
                return RedirectError(redirectUri, state, "access_denied", "The user has no access to the chosen tenant.");
            }

            tenantId = chosenTenantId;
        }
        // 2. Exactly one organization: use it.
        else if (userTenantIds.Count == 1)
        {
            tenantId = userTenantIds[0];
        }
        // 3. Several organizations: let the user choose.
        else if (userTenantIds.Count > 1)
        {
            return RedirectToPage("/SelectTenant", new { returnUrl = Request.Path + Request.QueryString });
        }
        // 4. Only one organization may use the client (client.TenantId), and the user is not in it:
        //    no access.
        else if (client.TenantId is not null)
        {
            return RedirectError(redirectUri, state, "access_denied", "The user has no access to the client's tenant.");
        }
        // 5. None: sign in without one. The tokens have no tenant_id and no roles, and the product
        //    lets the user create an organization (POST api/tenant) and signs them in again.
        else
        {
            tenantId = null;
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

    // Organizations where the user has roles in the client's product. A client of one organization
    // only (client.TenantId) counts only that one.
    private async Task<List<Guid>> GetUserTenantIdsAsync(Guid userId, Client client)
    {
        var tenantIds = await tenantService.GetUserTenantIdsAsync(userId, client.TargetResourceServerId);

        return client.TenantId is { } clientTenantId
            ? tenantIds.Where(t => t == clientTenantId).ToList()
            : tenantIds;
    }

    // The organization the user chose on the selection page (SelectTenant adds it to the address as
    // "tenant"), or null when the user has not chosen. An invalid value gives Guid.Empty, which the
    // user never belongs to.
    private Guid? GetChosenTenantId()
    {
        var value = Request.Query["tenant"].ToString();
        if (value == "")
        {
            return null;
        }

        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
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
