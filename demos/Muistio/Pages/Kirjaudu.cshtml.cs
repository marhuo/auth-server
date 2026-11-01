using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Muistio.Pages;

// Sends the user to the auth server. After sign-in the auth server returns to /signin-oidc, and the
// OIDC handler continues to /App.
public class KirjauduModel : PageModel
{
    public IActionResult OnGet() =>
        Challenge(new AuthenticationProperties { RedirectUri = "/App" });
}
