using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesDemo.Pages;

public class IndexModel : PageModel
{
    // Error message from a failed sign-in.
    [BindProperty(SupportsGet = true)]
    public string? Error { get; set; }

    public IActionResult OnGet() =>
        User.Identity?.IsAuthenticated == true ? Redirect("/App") : Page();
}
