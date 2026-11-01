using System.Security.Claims;

namespace Muistio;

// Muistio's own roles. The auth server gives them no meaning; Muistio decides what they allow:
// an admin may buy and invite users, a user may only use the app.
public static class Roles
{
    public const string Admin = "muistio_admin";
    public const string User = "muistio_user";

    public const string AdminPolicy = "Admin";
}

// Values the auth server puts in the ID token.
public static class ClaimsPrincipalExtensions
{
    // The tenant the user chose when signing in. Everything the user does, buying too, is for this tenant.
    // Pages under /App always have one (Program.cs sends a user without one to /Aloitus).
    public static Guid TenantId(this ClaimsPrincipal user) =>
        user.FindTenantId() ?? throw new InvalidOperationException("The ID token has no tenant_id.");

    // Null when the user has no workspace in Muistio yet: the auth server then signs the user in
    // without one (no tenant_id, no roles), and Muistio lets the user create one.
    public static Guid? FindTenantId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue("tenant_id"), out var id) ? id : null;

    public static string Email(this ClaimsPrincipal user) => user.Identity?.Name ?? "";

    public static List<string> RoleNames(this ClaimsPrincipal user) =>
        user.FindAll("role").Select(c => c.Value).ToList();

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Admin);
}
