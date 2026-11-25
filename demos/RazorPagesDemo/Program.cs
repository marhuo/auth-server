using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using RazorPagesDemo;
using RazorPagesDemo.Controllers;

var builder = WebApplication.CreateBuilder(args);
var authServer = builder.Configuration.GetSection("AuthServer");

var razorPages = builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/App");
    options.Conventions.AuthorizePage("/App/Users", Roles.TenantAdminPolicy);
    options.Conventions.AuthorizePage("/App/Domains", Roles.TenantAdminPolicy);
    options.Conventions.AuthorizePage("/CreateTenant");
});

// Development: a changed .cshtml file shows on page refresh, without a restart.
if (builder.Environment.IsDevelopment())
    razorPages.AddRazorRuntimeCompilation();
builder.Services.AddControllers(); // AccountController: sign-in actions without a page.

// Sign-in through the auth server.
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    // Own cookie name, so it does not mix with the auth server's cookie on localhost.
    .AddCookie(options =>
    {
        options.Cookie.Name = "RazorPagesDemo.Auth";

        // After a restart of the demo, everyone signs in again.
        options.Events.OnValidatePrincipal = async context =>
        {
            if (context.Principal?.FindFirst(StartupId.ClaimType)?.Value != StartupId.Value)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = authServer["Authority"];
        options.RequireHttpsMetadata = false; // The auth server runs on http in development.
        options.ClientId = authServer["WebClientId"];
        options.ClientSecret = authServer["WebClientSecret"];
        options.CallbackPath = "/signin-oidc"; // The client's redirect URI in the auth server.
        options.ResponseType = "code";
        options.UsePkce = true;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.MapInboundClaims = false;
        options.GetClaimsFromUserInfoEndpoint = true; // The user's tenants come from connect/userinfo.
        options.ClaimActions.MapJsonKey("tenants", "tenants"); // One claim per tenant: {"id":...,"name":...}
        options.TokenValidationParameters.NameClaimType = "email";
        options.TokenValidationParameters.RoleClaimType = "role";

        // Tenant switch: sends the chosen tenant to the auth server.
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            if (context.Properties.Items.TryGetValue(AccountController.TenantParameter, out var tenantId))
                context.ProtocolMessage.SetParameter(AccountController.TenantParameter, tenantId);
            return Task.CompletedTask;
        };

        // Marks the sign-in with this run of the demo.
        options.Events.OnTokenValidated = context =>
        {
            (context.Principal?.Identity as ClaimsIdentity)?.AddClaim(new Claim(StartupId.ClaimType, StartupId.Value));
            return Task.CompletedTask;
        };

        // A failed sign-in is shown on the front page.
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/?error=" + Uri.EscapeDataString(context.Failure?.Message ?? "Kirjautuminen epäonnistui."));
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Roles.TenantAdminPolicy, policy => policy.RequireRole(Roles.TenantAdmin));

builder.Services.AddHttpClient<AuthServerApi>(client =>
    client.BaseAddress = new Uri(authServer["Authority"]!));

builder.Services.AddSingleton(new TenantStore(Path.Combine(builder.Environment.ContentRootPath, "data", "tenants.json")));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();

// A user without a tenant is sent to create one.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/App")
        && context.User.Identity?.IsAuthenticated == true
        && context.User.FindTenantId() is null)
    {
        context.Response.Redirect("/CreateTenant");
        return;
    }

    await next();
});

app.UseAuthorization();
app.MapRazorPages();
app.MapControllers();

app.Run();
