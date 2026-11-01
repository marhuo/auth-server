using AuthServer.Web.Application.Roles;
using AuthServer.Web.Persistence;
using Microsoft.AspNetCore.Identity;
namespace AuthServer.Web.Data;

public static class DevDatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var passwordHasher = services.GetRequiredService<IPasswordHasher<UserEntity>>();
        var db = services.GetRequiredService<AuthDbContext>();

       

        await db.CreateTenantsAsync([
            "Acme Corporation", "Globex", "Initech", "Umbrella Group", "Stark Industries",
            "Wayne Enterprises", "Hooli", "Pied Piper", "Vandelay Industries", "Soylent Corp",
            "Cyberdyne Systems", "Tyrell Corporation", "Wonka Industries", "Oscorp", "Massive Dynamic",
            "Aperture Science", "Black Mesa", "Nakatomi Trading", "Gringotts", "Monsters Inc",
        ]);

        await CreateReactDemo(services);
    }

    private static async Task<List<(Guid TenantId, string TenantName)>> CreateTenantsAsync(this AuthDbContext db, params string[] tenantNames)
    {
        var tenants = new List<(Guid TenantId, string TenantName)>();

        foreach (var tenantName in tenantNames)
        {
            var tenantId = await db.CreateTenantIfNotExistsAsync(tenantName, ToSlug(tenantName));
        }

        return tenants;
    }

    private static string ToSlug(string value) =>
        string.Join('-', value.Trim().ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static async Task CreateReactDemo(IServiceProvider services)
    {
        var passwordHasher = services.GetRequiredService<IPasswordHasher<UserEntity>>();
        var db = services.GetRequiredService<AuthDbContext>();

        var resourceServerId = await db.CreateResourceServerIfNotExistsAsync("react-demo", "React demo api");
        var scopeId = await db.CreateScopeIfNotExistsAsync("react-demo.api", "React demo API", resourceServerId, false);

        var clientScopes = await db.GetScopeIdsByNamesAsync(["openid", "profile", "offline_access", "email", "react-demo.api"]);
        var managerRoleId = await db.CreateRoleIfNotExistsAsync(resourceServerId, "react_demo_manager", "React demo app manager");
        var workerRoleId = await db.CreateRoleIfNotExistsAsync(resourceServerId, "react_demo_worker", "React demo app worker");

        var userId = await db.CreateUserIfNotExistsAsync(passwordHasher, "matti", "matti@example.com", "matti12345678");

        var acmeId = await db.GetTenantIdByNameAsync("Acme Corporation") ?? throw new InvalidOperationException("Acme Corporation id not found");
        var globexId = await db.GetTenantIdByNameAsync("Globex") ?? throw new InvalidOperationException("Gobex id not found");

        await db.CreateUserRoleIfNotExistAsync(userId, managerRoleId, acmeId);
        await db.CreateUserRoleIfNotExistAsync(userId, workerRoleId, globexId);

        await db.CreatePublicApplicationClientIfNotExistsAsync(
           clientId: "react-demo-ui",
           displayName: "React demo ui",
           tenantId: null,
           resourceServerId: resourceServerId,
           redirectUris: ["https://localhost:4010/callback"],
           postLogoutRedirectUris: ["https://localhost:4010/"],
           allowedScopeIds: clientScopes
       );


    }

}