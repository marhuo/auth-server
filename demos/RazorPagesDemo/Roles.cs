using System.Security.Claims;
using System.Text.Json;

namespace RazorPagesDemo;

// The demo's roles. A tenant admin may buy and invite users.
public static class Roles
{
    public const string TenantAdmin = "razor_pages_demo_tenant_admin";
    public const string User = "razor_pages_demo_user";

    public const string TenantAdminPolicy = "TenantAdmin";
}

// Reads the signed-in user's details from the token.
public static class ClaimsPrincipalExtensions
{
    // The user's tenant. Throws if there is none.
    public static Guid TenantId(this ClaimsPrincipal user) =>
        user.FindTenantId() ?? throw new InvalidOperationException("The ID token has no tenant_id.");

    // The user's tenant, or null if the user has not created one yet.
    public static Guid? FindTenantId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue("tenant_id"), out var id) ? id : null;

    public static string Email(this ClaimsPrincipal user) => user.Identity?.Name ?? "";

    public static List<string> RoleNames(this ClaimsPrincipal user) =>
        user.FindAll("role").Select(c => c.Value).ToList();

    public static bool IsTenantAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.TenantAdmin);

    // The user's tenants in this app, from connect/userinfo.
    public static List<UserTenant> Tenants(this ClaimsPrincipal user) =>
        user.FindAll("tenants").Select(c => JsonSerializer.Deserialize<UserTenant>(c.Value, JsonOptions)!).ToList();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

public record UserTenant(Guid Id, string Name);
