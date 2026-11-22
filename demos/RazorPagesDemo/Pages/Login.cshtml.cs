using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesDemo.Pages;

// Opens the auth server's login page. Afterwards the user goes to /App.
public class LoginModel : PageModel
{
    public IActionResult OnGet() =>
        Challenge(new OpenIdConnectChallengeProperties { RedirectUri = "/App" });
}
