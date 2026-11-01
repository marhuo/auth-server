using AuthServer.Web.Application.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace AuthServer.Web.Pages
{
    public class LoginModel(UserService userService) : PageModel
    {
        [BindProperty]
        public string Username { get; set; } = "";

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
            // 3. The user enters their username and password on this page.
            // 4. After a successful login, the user must be sent back to /connect/authorize
            //    with the same parameters. Otherwise the OAuth flow would stay unfinished
            //    and the app would never receive a token.

            // Reject external return URLs (open redirect protection).
            if (!string.IsNullOrWhiteSpace(ReturnUrl) && !Url.IsLocalUrl(ReturnUrl))
                return LocalRedirect("/Login");


            var (status, user) = await userService.CheckPasswordAsync(Username, Password);

            if (status != LoginStatus.Success)
            {
                ModelState.AddModelError("", status == LoginStatus.LockedOut
                    ? "Tili on tilapäisesti lukittu. Yritä myöhemmin uudelleen."
                    : "Virheellinen käyttäjätunnus tai salasana.");
                return Page();
            }

            // cookieen vain globaalit roolit (esim. AuthServerAdmin), ei tenant-rooleja
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user!.Id.ToString()),
                new(ClaimTypes.Name, user.UserName)
            };

            claims.AddRange((await userService.GetAuthServerRolesAsync(user.Id)).Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = false });



            // Resume the authorization request, or go to the admin area if there was none.
            return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/Admin/Users" : ReturnUrl);
        }
    }
}
