namespace AuthServer.Web.Setup;

public static class CorsSetup
{
    // Not named AddCors: ASP.NET Core already has AddCors(IServiceCollection), and the call would be ambiguous.
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        return services;
    }
}
