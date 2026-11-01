using AuthServer.Application.OAuth.Services;
using AuthServer.Domain.Common;
using AuthServer.Domain.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Web.Pages;

// Sets the password with an invitation link (first password) or a reset link (forgotten password).
public class SetPasswordModel(UserAuthenticationService userService) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Token { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string ConfirmPassword { get; set; } = "";

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

        try
        {
            if (!await userService.SetPasswordWithTokenAsync(Token, Password))
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

        return RedirectToPage("PasswordSet");
    }
}
