using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesDemo.Pages;

// Signs in again to the chosen tenant. No password is asked.
public class SwitchTenantModel : PageModel
{
    public IActionResult OnPost(Guid tenantId)
    {
        var properties = new OpenIdConnectChallengeProperties { RedirectUri = "/App" };
        properties.Items["tenant"] = tenantId.ToString();
        return Challenge(properties);
    }
}
