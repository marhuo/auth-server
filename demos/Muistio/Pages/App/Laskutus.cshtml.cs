using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Muistio.Pages.App;

public class LaskutusModel(WorkspaceStore workspaces) : PageModel
{
    public Workspace? Workspace { get; private set; }

    public void OnGet()
    {
        Workspace = workspaces.Get(User.TenantId());
    }

    // Phase 1: no payment, the subscription is marked paid at once. The buyer is the tenant in the
    // token; the role is checked here too, because hiding the button is not enough.
    public IActionResult OnPost()
    {
        if (!User.IsAdmin())
            return Forbid();

        var tenantId = User.TenantId();
        var workspace = workspaces.Get(tenantId) ?? new Workspace(tenantId, "Tuntematon työtila", false, null);
        workspaces.Save(workspace with { Paid = true, PaidAt = DateTimeOffset.UtcNow });

        return RedirectToPage();
    }
}
