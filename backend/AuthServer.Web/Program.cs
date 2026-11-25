//Program
using AuthServer.Application;
using AuthServer.Infrastructure;
using AuthServer.Application.OAuth;
using AuthServer.Domain.OAuth;
using AuthServer.Web.Authentication;
using AuthServer.Web.Setup;
using AuthServer.Infrastructure.Persistence;
using AuthServer.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var razorPages = builder.Services.AddRazorPages();

// Development: a changed .cshtml file shows on page refresh, without a restart.
if (builder.Environment.IsDevelopment())
    razorPages.AddRazorRuntimeCompilation();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddErrorResponses();

builder.Services.AddLogging(builder.Configuration.GetConnectionString("Logs")!);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Default")!);
builder.Services.AddBackgroundJobs(builder.Configuration.GetConnectionString("Jobs")!);

builder.Services.AddAuthServerAuthentication(builder.Configuration["Tokens:Issuer"]!);
builder.Services.AddGoogleSignIn(builder.Configuration);
builder.Services.AddAuthorizationPolicies();

// Keys are in the main database, together with the signing keys they protect.
builder.Services.AddDataProtection()
    .SetApplicationName("AuthServer")
    .PersistKeysToDbContext<AuthDbContext>();
builder.Services.Configure<AuthTokenOptions>(builder.Configuration.GetSection("Tokens"));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddRateLimits();
builder.Services.AddTurnstile(builder.Configuration);
builder.Services.AddCorsPolicy();

var app = builder.Build();
app.UseErrorResponses();
app.UseRequestLogging();

using (var scope = app.Services.CreateScope())
{
    var signingKeys = scope.ServiceProvider.GetRequiredService<ISigningKeyManager>();
    await signingKeys.EnsureActiveKeyAsync();
    await CommonDatabaseSeeder.SeedAsync(scope.ServiceProvider);

    if (app.Environment.IsDevelopment())
        await DevDatabaseSeeder.SeedAsync(scope.ServiceProvider);
}
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseLogViewer(AuthorizationPolicies.Admin);
app.UseBackgroundJobs(AuthorizationPolicies.Admin);

app.MapGet("/", (HttpContext context) =>
    Results.Redirect(context.User.Identity?.IsAuthenticated == true ? "/admin" : "/Login"));

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapControllers();
app.MapFallbackToFile("/admin/{*path}", "admin/index.html")
   .RequireAuthorization(AuthorizationPolicies.Admin);
app.Run();