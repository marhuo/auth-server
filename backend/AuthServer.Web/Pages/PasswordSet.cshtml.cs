using AuthServer.Application.OAuth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Web.Pages;

// Shown after the password has been set. The auth server cannot start a sign-in for a product itself,
// so when the invitation told the client (clientId), the page shows a button to the client's initiate
// login URI; otherwise the user goes to the app themselves.
public class PasswordSetModel(ClientAuthenticationService clientService) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? ClientId { get; set; }

    public string? AppName { get; private set; }
    public string? LoginUri { get; private set; }

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
            return;

        var client = await clientService.FindActiveAsync(ClientId);
        if (client?.InitiateLoginUri is null)
            return;

        AppName = client.DisplayName;
        LoginUri = client.InitiateLoginUri;
    }
}
