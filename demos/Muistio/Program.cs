using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Muistio;

var builder = WebApplication.CreateBuilder(args);
var authServer = builder.Configuration.GetSection("AuthServer");

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/App");
    options.Conventions.AuthorizePage("/App/Kayttajat", Roles.AdminPolicy);
    options.Conventions.AuthorizePage("/Aloitus");
});

// Sign-in: the user signs in on the auth server (OIDC), the demo keeps the result in its own cookie.
// The ID token tells who the user is (email), in which tenant they are (tenant_id) and their roles in
// that tenant (role).
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    // Own cookie name: in development the auth server also runs on localhost, and browsers do not
    // separate cookies by port. With the default name (.AspNetCore.Cookies) the two apps would
    // overwrite each other's sign-in.
    .AddCookie(options =>
    {
        options.Cookie.Name = "Muistio.Auth";

        // Sign-ins from before the latest restart are rejected, so restarting Muistio starts from a
        // clean state (WorkspaceStore is in memory too). In development the auth server's database is
        // recreated often, and an old sign-in would show users and tenants that no longer exist.
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
        options.ResponseType = "code";
        options.UsePkce = true;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.MapInboundClaims = false;
        options.GetClaimsFromUserInfoEndpoint = false;
        options.TokenValidationParameters.NameClaimType = "email";
        options.TokenValidationParameters.RoleClaimType = "role";

        // "Try it free" (Rekisteroidy page) asks the auth server for its registration page instead of
        // the login page (prompt=create).
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            if (context.Properties.Items.ContainsKey(Muistio.Pages.RekisteroidyModel.SignupKey))
                context.ProtocolMessage.Prompt = "create";
            return Task.CompletedTask;
        };

        // Marks the sign-in with this run of Muistio (see OnValidatePrincipal above).
        options.Events.OnTokenValidated = context =>
        {
            (context.Principal?.Identity as ClaimsIdentity)?.AddClaim(new Claim(StartupId.ClaimType, StartupId.Value));
            return Task.CompletedTask;
        };

        // E.g. access_denied when the user has no Muistio role in any tenant: show it on the front page.
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/?error=" + Uri.EscapeDataString(context.Failure?.Message ?? "Kirjautuminen epäonnistui."));
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Roles.AdminPolicy, policy => policy.RequireRole(Roles.Admin));

builder.Services.AddHttpClient<AuthServerApi>(client =>
    client.BaseAddress = new Uri(authServer["Authority"]!));

builder.Services.AddSingleton<WorkspaceStore>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();

// A signed-in user without a workspace (no tenant_id in the token) creates one first.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/App")
        && context.User.Identity?.IsAuthenticated == true
        && context.User.FindTenantId() is null)
    {
        context.Response.Redirect("/Aloitus");
        return;
    }

    await next();
});

app.UseAuthorization();
app.MapRazorPages();

app.Run();
