using AuthServer.Web.Pages;
using System.Threading.RateLimiting;

namespace AuthServer.Web.Setup;

public static class RateLimitSetup
{
    // "Forgot password" may send at most 10 reset emails per IP address in 15 minutes. Only form posts
    // count; opening the page does not. Behind a reverse proxy, RemoteIpAddress needs forwarded headers.
    public static IServiceCollection AddRateLimits(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(ForgotPasswordModel.PasswordResetRateLimit, context =>
                HttpMethods.IsPost(context.Request.Method)
                    ? RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(15) })
                    : RateLimitPartition.GetNoLimiter("get"));
        });

        return services;
    }
}
