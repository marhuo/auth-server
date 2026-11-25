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
    public const string LoginHintParameter = "login_hint";

    // The auth server's login page. Afterwards the user goes to /App. The invitation email link has
    // login_hint (the invited email): passed on, so the auth server fills in the email.
    [HttpGet]
    public IActionResult Login([FromQuery(Name = LoginHintParameter)] string? loginHint)
    {
        var properties = new OpenIdConnectChallengeProperties { RedirectUri = "/App" };
        if (!string.IsNullOrEmpty(loginHint))
            properties.Items[LoginHintParameter] = loginHint;
        return Challenge(properties);
    }

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

    // Signs in again to the chosen tenant. The auth server remembers the user, so nothing is asked.
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SwitchTenant(Guid tenantId)
    {
        var properties = new OpenIdConnectChallengeProperties { RedirectUri = "/App" };
        properties.Items[TenantParameter] = tenantId.ToString();
        return Challenge(properties);
    }
}
