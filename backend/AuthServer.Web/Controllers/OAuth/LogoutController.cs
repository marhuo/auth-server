using AuthServer.Domain.Clients;
using AuthServer.Application.OAuth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AuthServer.Web.Controllers.OAuth;

public class LogoutController(ClientAuthenticationService clientService) : Controller
{
    [HttpGet("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var redirectUri = Request.Query["post_logout_redirect_uri"].ToString();
        var state = Request.Query["state"].ToString();

        if (string.IsNullOrEmpty(redirectUri) || !await clientService.IsPostLogoutRedirectUriRegisteredAsync(redirectUri))
        {
            return RedirectToPage("/Login");
        }

        return Redirect(string.IsNullOrEmpty(state)
            ? redirectUri
            : QueryHelpers.AddQueryString(redirectUri, "state", state));
    }
}