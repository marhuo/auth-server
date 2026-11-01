using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Muistio.Pages.App;

// Only for muistio_admin (Program.cs: AuthorizePage with the Admin policy).
public class KayttajatModel(AuthServerApi authServer) : PageModel
{
    [BindProperty]
    public string Email { get; set; } = "";

    public string? Error { get; private set; }
    public string? InvitedEmail { get; private set; }

    public async Task OnPostAsync()
    {
        Email = Email.Trim();
        if (Email == "")
        {
            Error = "Anna sähköposti.";
            return;
        }

        try
        {
            await authServer.InviteUserAsync(User.TenantId(), Email, Roles.User);
            InvitedEmail = Email;
            Email = "";
        }
        catch (AuthServerApiException e)
        {
            Error = e.Message;
        }
    }
}
