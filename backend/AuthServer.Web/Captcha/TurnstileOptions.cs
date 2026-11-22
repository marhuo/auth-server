namespace AuthServer.Web.Captcha;

// Cloudflare Turnstile keys (section "Turnstile"). Development uses Cloudflare's test keys.
public class TurnstileOptions
{
    public string SiteKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
}
