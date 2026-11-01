using AuthServer.Application.OAuth.Services;
using AuthServer.Domain.Common;
using AuthServer.Domain.Users;
using AuthServer.Web.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Web.Pages;

// Sets the password with an invitation or registration link (first password) or a reset link
// (forgotten password).
public class SetPasswordModel(UserAuthenticationService userService, UserSession userSession, TimeProvider timeProvider) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Token { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string ConfirmPassword { get; set; } = "";

    // Set by a registration link: where the user continues after setting the password, e.g. the
    // product's sign-in request.
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    // Set by an invitation link: the client the user signs in with. Passed on to the PasswordSet page,
    // which shows a button to its initiate login URI.
    [BindProperty(SupportsGet = true)] public string? ClientId { get; set; }

    // Null when the link is not valid (unknown, used or expired).
    public PasswordTokenTypes? TokenType { get; private set; }

    public async Task OnGetAsync()
    {
        TokenType = await userService.GetUsablePasswordTokenTypeAsync(Token);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        TokenType = await userService.GetUsablePasswordTokenTypeAsync(Token);
        if (TokenType is null) return Page();

        if (Password != ConfirmPassword)
        {
            ModelState.AddModelError("", "Salasanat eivät täsmää.");
            return Page();
        }

        User? user;
        try
        {
            user = await userService.SetPasswordWithTokenAsync(Token, Password);
            if (user is null)
            {
                TokenType = null;
                return Page();
            }
        }
        catch (ValidationException)
        {
            ModelState.AddModelError("", "Salasana ei täytä vaatimuksia.");
            return Page();
        }

        // The person who set the password is the one at this browser: sign them in, replacing any earlier
        // sign-in (e.g. the admin who sent the invitation). Then the next sign-in to a product is theirs,
        // and they do not have to type the new password again.
        if (user.CanSignIn(timeProvider.GetUtcNow()))
        {
            await userSession.SignInAsync(HttpContext, user);

            // Only local addresses (open redirect protection).
            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("PasswordSet", new { clientId = ClientId });
    }
}
