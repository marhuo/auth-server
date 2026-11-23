using AuthServer.Application.OAuth.Services;
using AuthServer.Domain.Common;
using AuthServer.Domain.Users;

namespace AuthServer.Web.Authentication;

// Signs in a user that an external service (Google, or the development test page) has identified.
public class ExternalSignIn(UserAuthenticationService userService, UserSession userSession)
{
    // Returns an error message for the user, or null when the user is signed in.
    public async Task<string?> SignInAsync(HttpContext httpContext, ExternalIdentity identity)
    {
        try
        {
            var (status, user) = await userService.SignInExternalAsync(identity);

            switch (status)
            {
                case ExternalSignInStatus.Success:
                    await userSession.SignInAsync(httpContext, user!);
                    return null;
                case ExternalSignInStatus.Locked:
                    return "Tili on lukittu. Ota yhteyttä ylläpitäjään.";
                default:
                    return "Sähköpostiosoitettasi ei ole vahvistettu. Kirjaudu sähköpostilla ja salasanalla.";
            }
        }
        catch (ValidationException)
        {
            // E.g. an email address that is not valid here.
            return "Kirjautuminen epäonnistui.";
        }
    }
}
