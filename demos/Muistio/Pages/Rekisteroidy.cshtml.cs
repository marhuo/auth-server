using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Muistio.Pages;

// "Try it free": sends the user to the auth server's registration page (prompt=create, Program.cs).
// After registering and setting the password the user comes back signed in, without a workspace,
// and /App sends them to the Aloitus page to create one.
public class RekisteroidyModel : PageModel
{
    public const string SignupKey = "signup";

    public IActionResult OnGet()
    {
        var properties = new AuthenticationProperties { RedirectUri = "/App" };
        properties.Items[SignupKey] = "true";
        return Challenge(properties);
    }
}
