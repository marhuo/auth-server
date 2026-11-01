using AuthServer.Web.Persistence;

namespace AuthServer.Web.Data;

public static class CommonDatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var authDbContext = services.GetRequiredService<AuthDbContext>();
        await authDbContext.CreateScopeIfNotExistsAsync("openid", "OpenID Connect scope", null);
        await authDbContext.CreateScopeIfNotExistsAsync("email", "Email scope", null);
        await authDbContext.CreateScopeIfNotExistsAsync("profile", "User profile scope", null);
        await authDbContext.CreateScopeIfNotExistsAsync("offline_access", "Offline access with refresh tokens", null);
    }

  
}
