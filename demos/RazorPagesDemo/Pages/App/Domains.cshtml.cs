using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesDemo.Pages.App;

// The tenant's domains in the auth server. Only for tenant admins.
public class DomainsModel(AuthServerApi authServer) : PageModel
{
    [BindProperty]
    public string Name { get; set; } = "";

    public List<TenantDomain> Domains { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task OnGetAsync() => await LoadAsync();

    public Task<IActionResult> OnPostAddAsync() =>
        RunAsync(() => authServer.AddDomainAsync(User.TenantId(), Name.Trim()));

    public Task<IActionResult> OnPostVerifyAsync(Guid id) =>
        RunAsync(() => authServer.VerifyDomainAsync(User.TenantId(), id));

    public Task<IActionResult> OnPostDeleteAsync(Guid id) =>
        RunAsync(() => authServer.DeleteDomainAsync(User.TenantId(), id));

    // Calls the auth server and shows its error message on the page.
    private async Task<IActionResult> RunAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (AuthServerApiException e)
        {
            Error = e.Message;
            await LoadAsync();
            return Page();
        }

        return RedirectToPage();
    }

    private async Task LoadAsync() => Domains = await authServer.GetDomainsAsync(User.TenantId());
}
