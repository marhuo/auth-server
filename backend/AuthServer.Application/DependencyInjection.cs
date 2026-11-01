using AuthServer.Application.Clients.Services;
using AuthServer.Application.Common.Services;
using AuthServer.Application.OAuth.Services;
using AuthServer.Application.ResourceServers.Services;
using AuthServer.Application.Roles.Services;
using AuthServer.Application.Scopes.Services;
using AuthServer.Application.Tenants.Services;
using AuthServer.Application.Users.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AuthServer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ClientService>();
        services.AddScoped<ClientValidator>();
        services.AddScoped<ResourceServerService>();
        services.AddScoped<ResourceServerValidator>();
        services.AddScoped<RoleService>();
        services.AddScoped<RoleValidator>();
        services.AddScoped<ScopeService>();
        services.AddScoped<ScopeValidator>();
        services.AddScoped<TenantService>();
        services.AddScoped<TenantValidator>();
        services.AddScoped<UserService>();
        services.AddScoped<UserValidator>();
        services.AddScoped<AuthorizationCodeService>();
        services.AddScoped<ClientAuthenticationService>();
        services.AddScoped<UserAuthenticationService>();
        services.AddScoped<TokenService>();
        services.AddScoped<ExpiredTokenCleanupService>();

        return services;
    }
}
