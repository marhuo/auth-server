using AuthServer.Application.OAuth.Services;
using AuthServer.Web.Captcha;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthServer.Web.Pages;

// "Forgot password": asks for the email and sends a reset link. Always shows the same confirmation,
// so the page cannot be used to find out which emails have an account. Rate limited per IP address
// (Setup/RateLimitSetup.cs, PasswordResetRateLimit) and protected by Cloudflare Turnstile, so it
// cannot be used to send emails in bulk.
[EnableRateLimiting(PasswordResetRateLimit)]
public class ForgotPasswordModel(UserAuthenticationService userService, TurnstileVerifier turnstile) : PageModel
{
    public const string PasswordResetRateLimit = "password-reset";

    [BindProperty] public string Email { get; set; } = "";

    public bool Sent { get; private set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await turnstile.VerifyAsync(HttpContext))
        {
            ModelState.AddModelError("", "Vahvistus epäonnistui. Yritä uudelleen.");
            return Page();
        }

        await userService.RequestPasswordResetAsync(Email);
        Sent = true;
        return Page();
    }
}
