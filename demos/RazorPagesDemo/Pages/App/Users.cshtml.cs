using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesDemo.Pages.App;

// Only for tenant admins.
public class UsersModel(AuthServerApi authServer) : PageModel
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
