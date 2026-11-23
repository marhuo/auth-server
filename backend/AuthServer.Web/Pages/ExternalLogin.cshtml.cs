using System.Security.Claims;
using AuthServer.Domain.Users;
using AuthServer.Web.Authentication;
using AuthServer.Web.Setup;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Web.Pages;

// Sign-in with Google: OnGet sends the user to Google, OnGetCallback receives the answer.
public class ExternalLoginModel(ExternalSignIn externalSignIn, IConfiguration configuration) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? Error { get; private set; }

    // /ExternalLogin: sends the user to Google's sign-in page.
    public IActionResult OnGet()
    {
        // No Google keys, or a return address outside the auth server.
        if (!AuthenticationSetup.IsGoogleConfigured(configuration) || !IsValidReturnUrl())
            return NotFound();

        var properties = new AuthenticationProperties
        {
            // After Google: OnGetCallbackAsync, which then continues the normal sign-in (returnUrl,
            // e.g. /connect/authorize?client_id=razor-pages-demo-web&...).
            RedirectUri = Url.Page("/ExternalLogin", "Callback", new { returnUrl = ReturnUrl })
        };

        // Used if the user cancels on Google's page (AuthenticationSetup, OnRemoteFailure).
        if (!string.IsNullOrWhiteSpace(ReturnUrl))
            properties.Items["returnUrl"] = ReturnUrl;

        // Redirect to Google.
        return Challenge(properties, AuthenticationSetup.GoogleScheme);
    }

    // /ExternalLogin?handler=Callback: the user is back from Google. Signs them in to the auth server.
    public async Task<IActionResult> OnGetCallbackAsync()
    {
        if (!AuthenticationSetup.IsGoogleConfigured(configuration) || !IsValidReturnUrl())
            return NotFound();

        var result = await HttpContext.AuthenticateAsync(AuthenticationSetup.ExternalScheme);
        await HttpContext.SignOutAsync(AuthenticationSetup.ExternalScheme);

        var sub = result.Principal?.FindFirstValue("sub");
        var email = result.Principal?.FindFirstValue("email");
        if (!result.Succeeded || sub is null || email is null)
        {
            Error = "Google-kirjautuminen epäonnistui.";
            return Page();
        }

        var identity = new ExternalIdentity(
            Provider: ExternalProviders.Google,
            ProviderUserId: sub,
            Email: email,
            EmailVerified: result.Principal!.FindFirstValue("email_verified") == "true");

        Error = await externalSignIn.SignInAsync(HttpContext, identity);
        if (Error is not null)
            return Page();

        return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/admin/users" : ReturnUrl);
    }

    // ReturnUrl is the app's sign-in request (/connect/authorize?...). Only local addresses
    // (open redirect protection).
    private bool IsValidReturnUrl() => string.IsNullOrWhiteSpace(ReturnUrl) || Url.IsLocalUrl(ReturnUrl);
}
