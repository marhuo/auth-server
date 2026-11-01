using AuthServer.Application.OAuth.Services;
using AuthServer.Domain.Users;
using AuthServer.Web.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Web.Pages
{
    public class LoginModel(UserAuthenticationService userService, UserSession userSession) : PageModel
    {
        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            // Login flow during an OAuth authorization code request:
            //
            // 1. The user opens the app. The app redirects them to
            //    /connect/authorize?client_id=app_client_id&scope=...&state=...
            // 2. The Authorize endpoint sees the user is not signed in and redirects to
            //    /Login?returnUrl=/connect/authorize?client_id=app_client_id&scope=...&state=...
            //    The original authorization request is stored in returnUrl so it is not lost.
            // 3. The user enters their email and password on this page.
            // 4. After a successful login, the user must be sent back to /connect/authorize
            //    with the same parameters. Otherwise the OAuth flow would stay unfinished
            //    and the app would never receive a token.

            // Reject external return URLs (open redirect protection).
            if (!string.IsNullOrWhiteSpace(ReturnUrl) && !Url.IsLocalUrl(ReturnUrl))
                return LocalRedirect("/Login");

            var (status, user) = await userService.LoginAsync(Email, Password);

            if (status != LoginStatus.Success)
            {
                ModelState.AddModelError("", status switch
                {
                    LoginStatus.LockedOut => "Tili on tilapäisesti lukittu. Yritä myöhemmin uudelleen.",
                    LoginStatus.Locked => "Tili on lukittu. Ota yhteyttä ylläpitäjään.",
                    _ => "Virheellinen sähköposti tai salasana."
                });
                return Page();
            }

            await userSession.SignInAsync(HttpContext, user!);

            // Resume the authorization request, or go to the admin area if there was none.
            return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/admin/users" : ReturnUrl);
        }
    }
}
