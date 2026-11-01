using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Muistio.Pages;

public class IndexModel : PageModel
{
    // Set when the sign-in failed, e.g. the user has no Muistio role in any tenant.
    [BindProperty(SupportsGet = true)]
    public string? Error { get; set; }

    public IActionResult OnGet() =>
        User.Identity?.IsAuthenticated == true ? Redirect("/App") : Page();
}
