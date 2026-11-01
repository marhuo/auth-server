//Program
using AuthServer.Web.Application.Clients;
using AuthServer.Web.Application.OAuth;
using AuthServer.Web.Application.Plans;
using AuthServer.Web.Application.ResourceServers;
using AuthServer.Web.Application.Roles;
using AuthServer.Web.Application.Scopes;
using AuthServer.Web.Application.Subscriptions;
using AuthServer.Web.Application.Tenants;
using AuthServer.Web.Application.Users;
using AuthServer.Web.Data;
using AuthServer.Web.Dto.Shared;
using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AuthServerAdmin");
});

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Key != "request" && e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(err => new ErrorDto(
                JsonNamingPolicy.CamelCase.ConvertName(e.Key.TrimStart('$', '.')),
                ErrorCode.InvalidFormat.ToString(),
                err.ErrorMessage,
                err.ErrorMessage)))
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
    });

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";
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
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<ResourceServerService>();
builder.Services.AddScoped<AuthorizationCodeService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<SigningKeyService>();
builder.Services.AddScoped<ScopeService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<PlanService>();
builder.Services.AddScoped<SubscriptionService>();

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

app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", (HttpContext context) =>
{
    return Results.Redirect(
        context.User.Identity?.IsAuthenticated == true
            ? "/Admin/Users"
            : "/Login");
});

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapControllers();

app.Run();