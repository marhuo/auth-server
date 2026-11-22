using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesDemo.Pages.App;

public class BillingModel(TenantStore tenants) : PageModel
{
    public Tenant? Tenant { get; private set; }

    public void OnGet()
    {
        Tenant = tenants.Get(User.TenantId());
    }

    // Marks the tenant as paid. Only tenant admins may buy.
    public IActionResult OnPost()
    {
        if (!User.IsTenantAdmin())
            return Forbid();

        var tenantId = User.TenantId();
        var tenant = tenants.Get(tenantId) ?? new Tenant(tenantId, "Tuntematon tenantti", false, null);
        tenants.Save(tenant with { Paid = true, PaidAt = DateTimeOffset.UtcNow });

        return RedirectToPage();
    }
}
