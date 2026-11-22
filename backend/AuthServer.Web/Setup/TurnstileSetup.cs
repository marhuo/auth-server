using AuthServer.Web.Captcha;

namespace AuthServer.Web.Setup;

public static class TurnstileSetup
{
    // Cloudflare Turnstile for the pages that send emails (Register, ForgotPassword). The keys are
    // required: the server does not start without them.
    public static IServiceCollection AddTurnstile(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TurnstileOptions>()
            .Bind(configuration.GetSection("Turnstile"))
            .Validate(o => o.SiteKey != "" && o.SecretKey != "", "Turnstile:SiteKey and Turnstile:SecretKey are required.")
            .ValidateOnStart();

        services.AddHttpClient<TurnstileVerifier>();

        return services;
    }
}
