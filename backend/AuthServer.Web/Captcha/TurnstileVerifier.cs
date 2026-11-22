using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace AuthServer.Web.Captcha;

// Checks the token that the Turnstile widget adds to the form. Fails if Cloudflare cannot be reached.
public class TurnstileVerifier(HttpClient http, IOptions<TurnstileOptions> options, ILogger<TurnstileVerifier> logger)
{
    private const string FormField = "cf-turnstile-response";
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public async Task<bool> VerifyAsync(HttpContext context)
    {
        var token = context.Request.Form[FormField].ToString();
        if (token == "")
            return false;

        var form = new Dictionary<string, string>
        {
            ["secret"] = options.Value.SecretKey,
            ["response"] = token
        };
        if (context.Connection.RemoteIpAddress is { } ip)
            form["remoteip"] = ip.ToString();

        try
        {
            using var response = await http.PostAsync(VerifyUrl, new FormUrlEncodedContent(form));
            var result = await response.Content.ReadFromJsonAsync<VerifyResponse>();
            if (result?.Success == true)
                return true;

            logger.LogInformation("Turnstile check failed: {ErrorCodes}", string.Join(", ", result?.ErrorCodes ?? []));
            return false;
        }
        catch (HttpRequestException e)
        {
            logger.LogWarning(e, "Turnstile could not be reached");
            return false;
        }
    }

    private record VerifyResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("error-codes")] string[]? ErrorCodes);
}
