using AuthServer.Web.Application.Clients;
using AuthServer.Web.Application.Tenants;
using AuthServer.Web.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;

namespace AuthServer.Web.Pages;

[Authorize]
public class SelectTenantModel(TenantService tenantService, ClientService clientService, TimeProvider timeProvider) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public List<Tenant> Tenants { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsValidReturnUrl())
        {
            return LocalRedirect("/");
        }

        var client = await FindClientAsync();
        if (client is null)
        {
            return LocalRedirect("/");
        }

        Tenants = await tenantService.GetUserTenantsAsync(GetUserId(), client.ResourceServerId, timeProvider.GetUtcNow());
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid tenantId)
    {
        if (!IsValidReturnUrl())
        {
            return LocalRedirect("/");
        }

        var client = await FindClientAsync();
        if (client is null)
        {
            return LocalRedirect("/");
        }

        var tenants = await tenantService.GetUserTenantsAsync(GetUserId(), client.ResourceServerId, timeProvider.GetUtcNow());
        if (!tenants.Any(t => t.Id == tenantId))
        {
            return Forbid();
        }

        return LocalRedirect(QueryHelpers.AddQueryString(ReturnUrl!, "tenant", tenantId.ToString()));
    }

    private async Task<Client?> FindClientAsync()
    {
        var queryStart = ReturnUrl!.IndexOf('?');
        if (queryStart < 0)
        {
            return null;
        }

        var query = QueryHelpers.ParseQuery(ReturnUrl[queryStart..]);
        return await clientService.FindActiveAsync(query["client_id"].ToString());
    }


    private bool IsValidReturnUrl() => !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl);

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}