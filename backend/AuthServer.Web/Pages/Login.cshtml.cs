using AuthServer.Application.OAuth.Services;
using AuthServer.Domain.Common;
using AuthServer.Web.Authentication;
using AuthServer.Web.Captcha;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace AuthServer.Web.Pages;

// Sign-in without a password: the user enters the email, gets a six-digit code by email and enters it.
// The first sign-in creates the user, so the same page also registers (prompt=create, AuthorizeController).
// Step 1 (Send) is protected by Cloudflare Turnstile; both steps are rate limited per IP address
// (Setup/RateLimitSetup.cs).
[EnableRateLimiting(SignInCodeRateLimit)]
public class LoginModel(
    UserAuthenticationService userService,
    UserSession userSession,
    ClientAuthenticationService clientService,
    TurnstileVerifier turnstile) : PageModel
{
    public const string SignInCodeRateLimit = "sign-in-code";

    // The email to sign in with.
    [BindProperty(SupportsGet = true)] public string Email { get; set; } = "";

    // The code sent to the email.
    [BindProperty(SupportsGet = true)] public string Code { get; set; } = "";

    // Where the user continues after signing in, e.g. the product's sign-in request.
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    // ?create=true: the heading is "Luo tunnus" instead of "Kirjaudu".
    [BindProperty(SupportsGet = true)] public bool Create { get; set; }

    // True: the page shows the code form. False: the email form.
    public bool ShowCodeForm { get; private set; }

    // The name of the app the user signs in to, e.g. "TuntiQ". Null when the page is opened directly.
    public string? AppName { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadAppNameAsync();
        ShowCodeForm = Code != "";
    }

    // Step 1: send the code.
    public async Task<IActionResult> OnPostSendAsync()
    {
        await LoadAppNameAsync();

        if (!IsValidReturnUrl())
            return RedirectToPage(new { returnUrl = (string?)null });

        if (!await turnstile.VerifyAsync(HttpContext))
        {
            ModelState.AddModelError("", "Vahvistus epäonnistui. Yritä uudelleen.");
            return Page();
        }

        try
        {
            await userService.SendSignInCodeAsync(Email, string.IsNullOrWhiteSpace(ReturnUrl) ? null : ReturnUrl);
        }
        catch (ValidationException)
        {
            ModelState.AddModelError("", "Tarkista sähköpostiosoite.");
            return Page();
        }

        ShowCodeForm = true;
        return Page();
    }

    // Step 2: check the code and sign in.
    public async Task<IActionResult> OnPostVerifyAsync()
    {
        await LoadAppNameAsync();

        if (!IsValidReturnUrl())
            return RedirectToPage(new { returnUrl = (string?)null });

        var user = await userService.SignInWithCodeAsync(Email, Code);
        if (user is null)
        {
            ShowCodeForm = true;
            ModelState.AddModelError("", "Koodi on väärä tai vanhentunut. Tarkista koodi tai pyydä uusi.");
            return Page();
        }

        await userSession.SignInAsync(HttpContext, user);

        // Resume the authorization request, or go to the admin area if there was none.
        return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/admin/users" : ReturnUrl);
    }

    // ReturnUrl is the app's sign-in request: /connect/authorize?client_id=tuntiq-web&...
    private async Task LoadAppNameAsync()
    {
        var query = ReturnUrl?.Split('?', 2).ElementAtOrDefault(1);
        var clientId = query is null ? "" : QueryHelpers.ParseQuery(query)["client_id"].ToString();
        if (clientId == "")
            return;

        var client = await clientService.FindActiveAsync(clientId);
        AppName = client?.DisplayName;
    }

    // Only local addresses (open redirect protection).
    private bool IsValidReturnUrl() => string.IsNullOrWhiteSpace(ReturnUrl) || Url.IsLocalUrl(ReturnUrl);
}
