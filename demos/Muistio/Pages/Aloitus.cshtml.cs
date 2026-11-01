using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Muistio.Pages;

// Signed in, but no workspace yet (Program.cs sends such users here). Requires sign-in, so Muistio
// knows who the user is and a workspace is never created for someone else's email.
public class AloitusModel(AuthServerApi authServer, WorkspaceStore workspaces) : PageModel
{
    [BindProperty]
    public string CompanyName { get; set; } = "";

    public string? Error { get; private set; }

    public IActionResult OnGet() =>
        User.FindTenantId() is not null ? Redirect("/App") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.FindTenantId() is not null)
            return Redirect("/App");

        CompanyName = CompanyName.Trim();
        if (CompanyName == "")
        {
            Error = "Anna nimi.";
            return Page();
        }

        try
        {
            // The email comes from the token: the user creates a workspace only for themselves.
            var tenantId = await authServer.CreateTenantAsync(CompanyName, User.Email());
            workspaces.Save(new Workspace(tenantId, CompanyName, Paid: false, PaidAt: null));
        }
        catch (AuthServerApiException e)
        {
            Error = e.Message;
            return Page();
        }

        // The current token has no workspace, so sign in again through the auth server. The user is
        // still signed in there, so no password is asked; now they have one workspace, and the new
        // token has its tenant_id and the role muistio_admin.
        return Challenge(new AuthenticationProperties { RedirectUri = "/App" });
    }
}
