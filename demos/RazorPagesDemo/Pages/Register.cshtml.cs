using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesDemo.Pages;

// "Try it free": opens the auth server's registration page.
public class RegisterModel : PageModel
{
    public IActionResult OnGet() =>
        Challenge(new OpenIdConnectChallengeProperties { RedirectUri = "/App", Prompt = "create" });
}
