using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesDemo.Pages;

// Lets a signed-in user without a tenant create one.
public class CreateTenantModel(AuthServerApi authServer, TenantStore tenants) : PageModel
{
    [BindProperty]
    public string Name { get; set; } = "";

    public string? Error { get; private set; }

    public IActionResult OnGet() =>
        User.FindTenantId() is not null ? Redirect("/App") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.FindTenantId() is not null)
            return Redirect("/App");

        Name = Name.Trim();
        if (Name == "")
        {
            Error = "Anna nimi.";
            return Page();
        }

        try
        {
            // The signed-in user becomes the tenant admin.
            var tenantId = await authServer.CreateTenantAsync(Name, User.Email());
            tenants.Save(new Tenant(tenantId, Paid: false, PaidAt: null));
        }
        catch (AuthServerApiException e)
        {
            Error = e.Message;
            return Page();
        }

        // Signs in again to get the new tenant into the token. No password is asked.
        return Challenge(new OpenIdConnectChallengeProperties { RedirectUri = "/App" });
    }
}
