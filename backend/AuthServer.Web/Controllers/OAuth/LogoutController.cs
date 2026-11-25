using AuthServer.Application.OAuth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AuthServer.Web.Controllers.OAuth;

public class LogoutController(ClientAuthenticationService clientService) : Controller
{
    // RP-initiated logout (OpenID Connect). The user is always signed out. The redirect back to the app
    // needs client_id: post_logout_redirect_uri must be registered for that client. Without them the
    // user goes to the login page.
    [HttpGet("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var clientId = Request.Query["client_id"].ToString();
        var redirectUri = Request.Query["post_logout_redirect_uri"].ToString();
        var state = Request.Query["state"].ToString();

        var client = clientId == "" ? null : await clientService.FindActiveAsync(clientId);
        if (client is null || !client.IsPostLogoutRedirectUriAllowed(redirectUri))
        {
            return RedirectToPage("/Login");
        }

        return Redirect(state == ""
            ? redirectUri
            : QueryHelpers.AddQueryString(redirectUri, "state", state));
    }
}
