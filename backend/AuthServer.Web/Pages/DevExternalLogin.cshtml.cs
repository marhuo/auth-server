using AuthServer.Domain.Users;
using AuthServer.Web.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Web.Pages;

// Development only: acts as Google, so external sign-in can be tested without Google.
public class DevExternalLoginModel(ExternalSignIn externalSignIn, IWebHostEnvironment environment) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    // Identifies the user, not the email: the same sub is always the same user.
    [BindProperty]
    public string Sub { get; set; } = "";

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public bool EmailVerified { get; set; } = true;

    public string? Error { get; private set; }

    public IActionResult OnGet() => IsAllowed() ? Page() : NotFound();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!IsAllowed())
            return NotFound();

        var identity = new ExternalIdentity(
            Provider: ExternalProviders.Google,
            ProviderUserId: Sub.Trim(),
            Email: Email.Trim(),
            EmailVerified: EmailVerified);

        Error = await externalSignIn.SignInAsync(HttpContext, identity);
        if (Error is not null)
            return Page();

        return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/admin/users" : ReturnUrl);
    }

    // Only in development, and only local return addresses (open redirect protection).
    private bool IsAllowed() =>
        environment.IsDevelopment() && (string.IsNullOrWhiteSpace(ReturnUrl) || Url.IsLocalUrl(ReturnUrl));
}
