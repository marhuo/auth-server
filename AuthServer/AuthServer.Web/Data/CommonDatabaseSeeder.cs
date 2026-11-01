using AuthServer.Web.Application.Roles;
using AuthServer.Web.Persistence;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Web.Data;

public static class CommonDatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var passwordHasher = services.GetRequiredService<IPasswordHasher<UserEntity>>();
        var db = services.GetRequiredService<AuthDbContext>();

        // Auth db resource server and admin user
        var authServerResourceServerId = await db.CreateResourceServerIfNotExistsAsync("auth-server", "Auth server", true);
        var authServerAdminRoleId = await db.CreateRoleIfNotExistsAsync(authServerResourceServerId, Roles.AuthServerAdmin, "Autentikointi palvelimen ylläpitäjä");
        var authServerAdminUserId = await db.CreateUserIfNotExistsAsync(passwordHasher, "admin", "marko.huotari@gmail.com", "admin12345");
        await db.CreateUserRoleIfNotExistAsync(authServerAdminUserId, authServerAdminRoleId, null);

        await db.CreateScopeIfNotExistsAsync("auth_server:api", "Auth server api", authServerResourceServerId, false);
        await db.CreateScopeIfNotExistsAsync("openid", "OpenID Connect scope", null, true);
        await db.CreateScopeIfNotExistsAsync("email", "Email scope", null, true);
        await db.CreateScopeIfNotExistsAsync("profile", "User profile scope", null, true);
        await db.CreateScopeIfNotExistsAsync("offline_access", "Offline access with refresh tokens", null, true);
    }
}
