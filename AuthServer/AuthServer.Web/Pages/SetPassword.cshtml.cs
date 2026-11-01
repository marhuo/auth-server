using AuthServer.Web.Application.Users;
using AuthServer.Web.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Web.Pages;

public class SetPasswordModel(UserTokenService tokenService, UserService userService) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Token { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string ConfirmPassword { get; set; } = "";
    public bool TokenValid { get; private set; }

    public async Task OnGetAsync()
    {
        TokenValid = await tokenService.FindUserIdByInviteTokenAsync(Token) is not null;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = await tokenService.FindUserIdByInviteTokenAsync(Token);
        TokenValid = userId is not null;
        if (userId is null) return Page();

        if (Password != ConfirmPassword)
        {
            ModelState.AddModelError("", "Salasanat eivät täsmää.");
            return Page();
        }

        try
        {
            await userService.ChangePasswordAsync(userId.Value, Password);
        }
        catch (ValidationException)
        {
            ModelState.AddModelError("", "Salasana ei täytä vaatimuksia.");
            return Page();
        }

        await tokenService.MarkUsedAsync(Token);
        return RedirectToPage("PasswordSet");
    }
}