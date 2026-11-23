using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace RazorPagesDemo.Controllers;

// Sign-in actions. Each one sends the browser to the auth server; none has a page of its own.
[Route("account/[action]")]
public class AccountController : Controller
{
    // The auth server's parameter for the chosen tenant. Program.cs adds it to the sign-in request.
    public const string TenantParameter = "tenant";

    // The auth server's login page. Afterwards the user goes to /App.
    [HttpGet]
    public IActionResult Login() =>
        Challenge(new OpenIdConnectChallengeProperties { RedirectUri = "/App" });

    // The auth server's registration page.
    [HttpGet]
    public IActionResult Register() =>
        Challenge(new OpenIdConnectChallengeProperties { RedirectUri = "/App", Prompt = "create" });

    // Signs out of the demo and of the auth server.
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout() =>
        SignOut(new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);

    // Signs in again to the chosen tenant. No password is asked.
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SwitchTenant(Guid tenantId)
    {
        var properties = new OpenIdConnectChallengeProperties { RedirectUri = "/App" };
        properties.Items[TenantParameter] = tenantId.ToString();
        return Challenge(properties);
    }
}
