using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Muistio;

// Calls the auth server's external API as Muistio's backend client muistio-backend (client
// credentials). The client's target is the auth server. The auth server accepts any product's roles,
// so giving Muistio's own roles is Muistio's responsibility.
public class AuthServerApi(HttpClient http, IConfiguration configuration)
{
    // Creates the tenant and invites the admin: a new user gets an invitation link to set a password
    // (in development the auth server writes the link to its log). Muistio tells which of its roles
    // the creator gets; the auth server does not know which role is the admin.
    public async Task<Guid> CreateTenantAsync(string name, string adminEmail)
    {
        var response = await SendAsync("api/tenant", new
        {
            name,
            adminEmail,
            adminRoleNames = new[] { Roles.Admin },
            // After setting the password, the invited user gets a button to Muistio's sign-in.
            clientId = configuration["AuthServer:WebClientId"]
        });

        var body = await response.Content.ReadFromJsonAsync<TenantCreateResponse>()
            ?? throw new InvalidOperationException("Empty response from api/tenant.");
        return body.TenantId;
    }

    public async Task InviteUserAsync(Guid tenantId, string email, string roleName)
    {
        await SendAsync("api/user/invite", new
        {
            email,
            tenantId,
            roleNames = new[] { roleName },
            clientId = configuration["AuthServer:WebClientId"]
        });
    }

    private async Task<HttpResponseMessage> SendAsync(string url, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

        var response = await http.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return response;

        // Validation errors (400) come as { errors: [{ key, code, messageEn, messageFi }] }.
        var text = await response.Content.ReadAsStringAsync();
        throw new AuthServerApiException(ReadErrorMessage(text) ?? $"{(int)response.StatusCode} {text}");
    }

    // A new token for each call: simple, and enough for a demo.
    private async Task<string> GetAccessTokenAsync()
    {
        var response = await http.PostAsync("connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = configuration["AuthServer:ApiClientId"]!,
            ["client_secret"] = configuration["AuthServer:ApiClientSecret"]!,
            ["scope"] = "auth_server:api"
        }));

        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new AuthServerApiException($"Tokenin haku epäonnistui: {text}");

        using var json = JsonDocument.Parse(text);
        return json.RootElement.GetProperty("access_token").GetString()!;
    }

    private static string? ReadErrorMessage(string text)
    {
        try
        {
            using var json = JsonDocument.Parse(text);
            var messages = json.RootElement.GetProperty("errors").EnumerateArray()
                .Select(e => e.GetProperty("messageFi").GetString());
            return string.Join(" ", messages);
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    private record TenantCreateResponse(Guid TenantId, Guid UserId, string Slug);
}

public class AuthServerApiException(string message) : Exception(message);
