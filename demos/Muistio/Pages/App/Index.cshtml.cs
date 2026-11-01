using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Muistio.Pages.App;

public class IndexModel(WorkspaceStore workspaces) : PageModel
{
    public Workspace? Workspace { get; private set; }

    public void OnGet()
    {
        Workspace = workspaces.Get(User.TenantId());
    }
}
