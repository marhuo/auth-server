using AuthServer.Domain.Roles;
using AuthServer.Domain.Scopes;
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
        services.AddScoped<ExternalSignIn>();

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Login";
                options.AccessDeniedPath = "/AccessDenied";

                // The user stays signed in for UserSession.Lifetime after the last visit (sliding).
                options.ExpireTimeSpan = UserSession.Lifetime;
                options.SlidingExpiration = true;
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

    public const string ExternalScheme = "External";
    public const string GoogleScheme = "Google";

    public static bool IsGoogleConfigured(IConfiguration configuration) =>
        !string.IsNullOrEmpty(configuration["Google:ClientId"]);

    // Google sign-in, only when Google:ClientId and Google:ClientSecret are set (user-secrets).
    // Google's answer is kept briefly in the External cookie until ExternalLogin reads it.
    public static IServiceCollection AddGoogleSignIn(this IServiceCollection services, IConfiguration configuration)
    {
        if (!IsGoogleConfigured(configuration))
            return services;

        services.AddAuthentication()
            .AddCookie(ExternalScheme, options =>
            {
                options.Cookie.Name = "AuthServer.External";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddOpenIdConnect(GoogleScheme, options =>
            {
                options.SignInScheme = ExternalScheme;
                options.Authority = "https://accounts.google.com";
                options.ClientId = configuration["Google:ClientId"];
                options.ClientSecret = configuration["Google:ClientSecret"];
                options.CallbackPath = "/signin-google"; // The redirect URI in Google Cloud Console.
                options.ResponseType = "code";
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("email");
                options.MapInboundClaims = false;

                // E.g. the user cancels on Google's page: back to the login page.
                options.Events.OnRemoteFailure = context =>
                {
                    var returnUrl = context.Properties?.Items.TryGetValue("returnUrl", out var url) == true ? url : null;
                    context.Response.Redirect(returnUrl is null ? "/Login" : $"/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
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
                    ctx.User.FindFirstValue("scope")?.Split(' ').Contains(SystemScopes.AuthServerApi) == true);
            });

        return services;
    }
}
