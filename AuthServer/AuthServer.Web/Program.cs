//Program
using AuthServer.Web.Application.Clients;
using AuthServer.Web.Application.Email;
using AuthServer.Web.Application.OAuth;
using AuthServer.Web.Application.ResourceServers;
using AuthServer.Web.Application.Roles;
using AuthServer.Web.Application.Scopes;
using AuthServer.Web.Application.Tenants;
using AuthServer.Web.Application.Users;
using AuthServer.Web.Data;
using AuthServer.Web.Dto.Shared;
using AuthServer.Web.Logging;
using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddLogging(builder.Configuration.GetConnectionString("Logs")!);
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Key != "request" && e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(err => new ErrorDto(
                JsonNamingPolicy.CamelCase.ConvertName(e.Key.TrimStart('$', '.')),
                ErrorCode.InvalidFormat.ToString(),
               "Value is missing or invalid.",
               "Arvo puuttuu tai on virheellinen.")))
            .ToList();

        return new BadRequestObjectResult(new ErrorResponseDto(errors));
    };
});

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
    options.UseSnakeCaseNamingConvention();
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AuthServerAdmin", policy =>
    {
        policy.RequireRole(Roles.AuthServerAdmin);
    })
    .AddPolicy("AuthServerApiScope", policy =>
    {
        policy.AddAuthenticationSchemes("Bearer");
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(ctx =>
            ctx.User.FindFirstValue("scope")?.Split(' ').Contains("auth_server:api") == true);
    });

builder.Services
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
    })
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Tokens:Issuer"];
        options.Audience = "auth-server";
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = false;
    }).AddJwtBearer("UserInfo", options =>
    {
        options.Authority = builder.Configuration["Tokens:Issuer"];
        options.TokenValidationParameters.ValidateAudience = false;
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddSingleton<IPasswordHasher<UserEntity>, PasswordHasher<UserEntity>>();


builder.Services.AddDataProtection().SetApplicationName("AuthServer");
builder.Services.Configure<AuthTokenOptions>(builder.Configuration.GetSection("Tokens"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AuthorizationCodeService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<SigningKeyService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<ClientValidator>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserValidator>();
builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<TenantValidator>();
builder.Services.AddScoped<ResourceServerService>();
builder.Services.AddScoped<ResourceServerValidator>();
builder.Services.AddScoped<ScopeService>();
builder.Services.AddScoped<ScopeValidator>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<RoleValidator>();
builder.Services.AddScoped<UserTokenService>();
builder.Services.AddScoped<IUserEmailSender, LoggingUserEmailSender>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();
var app = builder.Build();
app.UseExceptionHandler();
app.UseRequestLogging();

using (var scope = app.Services.CreateScope())
{
    var signingKeyService = scope.ServiceProvider.GetRequiredService<SigningKeyService>();
    await signingKeyService.EnsureActiveKeyAsync();
    await CommonDatabaseSeeder.SeedAsync(scope.ServiceProvider);
    await DevDatabaseSeeder.SeedAsync(scope.ServiceProvider);
}
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseLogViewer(Roles.AuthServerAdmin);

app.MapGet("/", (HttpContext context) =>
    Results.Redirect(context.User.Identity?.IsAuthenticated == true ? "/admin" : "/Login"));

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapControllers();
app.MapFallbackToFile("/admin/{*path}", "admin/index.html")
   .RequireAuthorization("AuthServerAdmin");
app.Run();