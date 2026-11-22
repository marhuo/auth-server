using AuthServer.Domain.Clients;
using AuthServer.Domain.ResourceServers;
using AuthServer.Domain.Roles;
using AuthServer.Domain.Scopes;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AuthServer.Domain.Users;
namespace AuthServer.Infrastructure.Persistence.Seed;

public static class DevDatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var db = services.GetRequiredService<AuthDbContext>();

       

        await db.CreateTenantsAsync([
            "Acme Corporation", "Globex", "Initech", "Umbrella Group", "Stark Industries",
            "Wayne Enterprises", "Hooli", "Pied Piper", "Vandelay Industries", "Soylent Corp",
            "Cyberdyne Systems", "Tyrell Corporation", "Wonka Industries", "Oscorp", "Massive Dynamic",
            "Aperture Science", "Black Mesa", "Nakatomi Trading", "Gringotts", "Monsters Inc",
        ]);

        await CreateReactDemo(services);
        await CreateRazorPagesDemo(db);
    }

    // demos/RazorPagesDemo: a product that is sold online (docs/saas-myynti-ja-rekisteroityminen.md).
    // No users or tenants: the demo creates them through the external API.
    private static async Task CreateRazorPagesDemo(AuthDbContext db)
    {
        var resourceServerId = await db.CreateResourceServerIfNotExistsAsync("razor-pages-demo", "Razor Pages demo");
        await db.CreateRoleIfNotExistsAsync(resourceServerId, "razor_pages_demo_tenant_admin", "Tenantin ylläpitäjä: saa ostaa ja kutsua käyttäjiä");
        await db.CreateRoleIfNotExistsAsync(resourceServerId, "razor_pages_demo_user", "Käyttäjä");

        // Signs in users.
        await db.CreateConfidentialClientIfNotExistsAsync(
            type: ClientTypes.ConfidentialApplication,
            clientId: "razor-pages-demo-web",
            displayName: "Razor Pages demo web",
            secret: "razor-pages-demo-web-secret",
            targetResourceServerId: resourceServerId,
            redirectUris: ["https://localhost:5101/signin-oidc"],
            postLogoutRedirectUris: ["https://localhost:5101/signout-callback-oidc"],
            allowedScopeIds: await db.GetScopeIdsByNamesAsync(["openid", "profile", "email"]),
            initiateLoginUri: "https://localhost:5101/Login");

        // The backend client that the app creates with every resource server (ResourceServerService.CreateAsync):
        // The demo's server calls the external API with it (POST api/tenant, POST api/user/invite).
        // Its target is the auth server.
        await db.CreateConfidentialClientIfNotExistsAsync(
            type: ClientTypes.ConfidentialMachine,
            clientId: ResourceServer.BackendClientId("razor-pages-demo"),
            displayName: ResourceServer.BackendClientDisplayName("Razor Pages demo"),
            secret: "razor-pages-demo-backend-secret",
            targetResourceServerId: (await db.ResourceServers.SingleAsync(r => r.IsAuthServer)).Id,
            redirectUris: [],
            postLogoutRedirectUris: [],
            allowedScopeIds: await db.GetScopeIdsByNamesAsync([SystemScopes.AuthServerApi]));
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
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var db = services.GetRequiredService<AuthDbContext>();

        var resourceServerId = await db.CreateResourceServerIfNotExistsAsync("react-demo", "React demo api");
        var scopeId = await db.CreateScopeIfNotExistsAsync("react-demo.api", "React demo API", resourceServerId, false);

        var clientScopes = await db.GetScopeIdsByNamesAsync(["openid", "profile", "offline_access", "email", "react-demo.api"]);
        var managerRoleId = await db.CreateRoleIfNotExistsAsync(resourceServerId, "react_demo_manager", "React demo app manager");
        var workerRoleId = await db.CreateRoleIfNotExistsAsync(resourceServerId, "react_demo_worker", "React demo app worker");

        var userId = await db.CreateUserIfNotExistsAsync(passwordHasher, "matti@example.com", "matti12345678");

        var acmeId = await db.GetTenantIdByNameAsync("Acme Corporation") ?? throw new InvalidOperationException("Acme Corporation id not found");
        var globexId = await db.GetTenantIdByNameAsync("Globex") ?? throw new InvalidOperationException("Gobex id not found");

        await db.CreateUserRoleIfNotExistAsync(userId, managerRoleId, acmeId);
        await db.CreateUserRoleIfNotExistAsync(userId, workerRoleId, globexId);

        await db.CreatePublicApplicationClientIfNotExistsAsync(
           clientId: "react-demo-ui",
           displayName: "React demo ui",
           tenantId: null,
           targetResourceServerId: resourceServerId,
           redirectUris: ["https://localhost:4010/callback"],
           postLogoutRedirectUris: ["https://localhost:4010/"],
           allowedScopeIds: clientScopes
       );


    }

}