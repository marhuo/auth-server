using AuthServer.Domain.Roles;
using AuthServer.Web.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace AuthServer.Web.Setup;

public static class AuthenticationSetup
{
    // Sign-in cookie (login page, admin UI) as the default scheme, plus bearer schemes for the
    // external API ("Bearer") and the userinfo endpoint ("UserInfo").
    public static IServiceCollection AddAuthServerAuthentication(this IServiceCollection services, string issuer)
    {
        services.AddScoped<UserSession>();

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Login";
                options.AccessDeniedPath = "/AccessDenied";
                options.Events.OnRedirectToLogin = ctx =>
                {
                    if (ctx.Request.Path.StartsWithSegments("/api"))
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    else
                        ctx.Response.Redirect(ctx.RedirectUri);
                    return Task.CompletedTask;
                };

                // Re-checks lock status and auth server roles periodically (Authentication:SessionValidationInterval).
                options.Events.OnValidatePrincipal = ctx =>
                    ctx.HttpContext.RequestServices.GetRequiredService<UserSession>().ValidateAsync(ctx);
            })
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = issuer;
                options.Audience = "auth-server";
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = false;
            })
            .AddJwtBearer("UserInfo", options =>
            {
                options.Authority = issuer;
                options.TokenValidationParameters.ValidateAudience = false;
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = false;
            });

        return services;
    }

    // Defines the policies named in AuthorizationPolicies.
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Admin, policy =>
            {
                policy.RequireRole(SystemRoles.AuthServerAdmin);
            })
            .AddPolicy(AuthorizationPolicies.ExternalApi, policy =>
            {
                policy.AddAuthenticationSchemes("Bearer");
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(ctx =>
                    ctx.User.FindFirstValue("scope")?.Split(' ').Contains("auth_server:api") == true);
            });

        return services;
    }
}
