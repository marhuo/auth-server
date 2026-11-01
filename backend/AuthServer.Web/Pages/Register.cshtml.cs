using AuthServer.Application.OAuth.Services;
using AuthServer.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthServer.Web.Pages;

// Self-registration: asks for the email and sends a link to set the password (the SetPassword page,
// as with invitations). Opened from the login page or by a product with prompt=create
// (AuthorizeController). Always shows the same confirmation, so the page cannot be used to find out
// which emails have an account. Rate limited per IP address like "forgot password"
// (Setup/RateLimitSetup.cs, RegistrationRateLimit).
[EnableRateLimiting(RegistrationRateLimit)]
public class RegisterModel(UserAuthenticationService userService) : PageModel
{
    public const string RegistrationRateLimit = "registration";

    [BindProperty] public string Email { get; set; } = "";

    // Where the user continues after setting the password, e.g. the product's sign-in request.
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public bool Sent { get; private set; }

    public IActionResult OnGet() =>
        IsValidReturnUrl() ? Page() : RedirectToPage(new { returnUrl = (string?)null });

    public async Task<IActionResult> OnPostAsync()
    {
        if (!IsValidReturnUrl())
            return RedirectToPage(new { returnUrl = (string?)null });

        try
        {
            await userService.RegisterAsync(Email, string.IsNullOrWhiteSpace(ReturnUrl) ? null : ReturnUrl);
        }
        catch (ValidationException)
        {
            ModelState.AddModelError("", "Tarkista sähköpostiosoite.");
            return Page();
        }

        Sent = true;
        return Page();
    }

    // Only local addresses (open redirect protection).
    private bool IsValidReturnUrl() => string.IsNullOrWhiteSpace(ReturnUrl) || Url.IsLocalUrl(ReturnUrl);
}
