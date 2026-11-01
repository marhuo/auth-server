using System.Globalization;
using System.Security.Claims;
using AuthServer.Application.OAuth.Services;
using AuthServer.Domain.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AuthServer.Web.Authentication;

// The auth server's sign-in cookie (login page, authorize endpoint, admin UI). It holds the user's id,
// email and auth server roles (e.g. AuthServerAdmin, not tenant roles) as they were at sign-in.
// ValidateAsync re-checks the user from the database at most once per validation interval, so locking
// a user or removing a role takes effect within the interval, not only when the cookie expires.
public class UserSession(UserAuthenticationService userService, TimeProvider timeProvider, IConfiguration configuration)
{
    private const string ValidatedAtKey = "validated_at";
    private static readonly TimeSpan DefaultValidationInterval = TimeSpan.FromMinutes(5);

    private TimeSpan ValidationInterval =>
        configuration.GetValue<TimeSpan?>("Authentication:SessionValidationInterval") ?? DefaultValidationInterval;

    public async Task SignInAsync(HttpContext httpContext, User user)
    {
        var properties = new AuthenticationProperties { IsPersistent = false };
        SetValidatedAt(properties, timeProvider.GetUtcNow());

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, await CreatePrincipalAsync(user), properties);
    }

    // CookieAuthenticationEvents.OnValidatePrincipal, called on every request with the cookie.
    public async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var now = timeProvider.GetUtcNow();

        if (GetValidatedAt(context.Properties) is { } validatedAt && now - validatedAt < ValidationInterval)
            return;

        var user = Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? await userService.GetByIdAsync(userId)
            : null;

        if (user is null || !user.CanSignIn(now))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        // The email and the auth server roles may have changed: renew the cookie with current values.
        context.ReplacePrincipal(await CreatePrincipalAsync(user));
        SetValidatedAt(context.Properties, now);
        context.ShouldRenew = true;
    }

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email)
        };

        // Only auth server roles (e.g. AuthServerAdmin), not tenant roles.
        claims.AddRange((await userService.GetAuthServerRoleNamesAsync(user.Id)).Select(r => new Claim(ClaimTypes.Role, r)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    private static DateTimeOffset? GetValidatedAt(AuthenticationProperties properties) =>
        properties.Items.TryGetValue(ValidatedAtKey, out var value)
        && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var validatedAt)
            ? validatedAt
            : null;

    private static void SetValidatedAt(AuthenticationProperties properties, DateTimeOffset now) =>
        properties.Items[ValidatedAtKey] = now.ToString("O", CultureInfo.InvariantCulture);
}
