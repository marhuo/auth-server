using AuthServer.Domain.Clients;
using AuthServer.Domain.OAuth;
using AuthServer.Domain.ResourceServers;
using AuthServer.Domain.Roles;
using AuthServer.Domain.Scopes;
using AuthServer.Domain.Tenants;
using AuthServer.Domain.Users;
using AuthServer.Infrastructure.OAuth;
using AuthServer.Infrastructure.Persistence;
using AuthServer.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthServer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AuthDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IResourceServerRepository, ResourceServerRepository>();
        services.AddScoped<IScopeRepository, ScopeRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordTokenRepository, PasswordTokenRepository>();
        services.AddScoped<IAuthorizationCodeRepository, AuthorizationCodeRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IUserEmailSender, LoggingUserEmailSender>();

        // One instance per scope serves both interfaces.
        services.AddScoped<SigningKeyService>();
        services.AddScoped<ITokenSigner>(sp => sp.GetRequiredService<SigningKeyService>());
        services.AddScoped<ISigningKeyManager>(sp => sp.GetRequiredService<SigningKeyService>());

        return services;
    }
}
