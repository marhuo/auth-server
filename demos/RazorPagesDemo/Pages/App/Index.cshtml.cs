using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesDemo.Pages.App;

public class IndexModel(TenantStore tenants) : PageModel
{
    public Tenant? Tenant { get; private set; }

    public void OnGet()
    {
        Tenant = tenants.Get(User.TenantId());
    }
}
