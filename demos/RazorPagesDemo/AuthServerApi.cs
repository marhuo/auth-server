using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RazorPagesDemo;

// Calls the auth server's API with the razor-pages-demo-backend client.
public class AuthServerApi(HttpClient http, IConfiguration configuration)
{
    // Creates a tenant and makes the user its admin.
    public async Task<Guid> CreateTenantAsync(string name, string adminEmail)
    {
        var response = await SendAsync(HttpMethod.Post, "api/tenant", new
        {
            name,
            adminEmail,
            adminRoleNames = new[] { Roles.TenantAdmin },
            // The invitation email links to the demo.
            clientId = configuration["AuthServer:WebClientId"]
        });

        var body = await response.Content.ReadFromJsonAsync<TenantCreateResponse>()
            ?? throw new InvalidOperationException("Empty response from api/tenant.");
        return body.TenantId;
    }

    public async Task InviteUserAsync(Guid tenantId, string email, string roleName)
    {
        await SendAsync(HttpMethod.Post, "api/user/invite", new
        {
            email,
            tenantId,
            roleNames = new[] { roleName },
            clientId = configuration["AuthServer:WebClientId"]
        });
    }

    public async Task<List<TenantDomain>> GetDomainsAsync(Guid tenantId)
    {
        var response = await SendAsync(HttpMethod.Get, $"api/tenant/{tenantId}/domain");
        return await response.Content.ReadFromJsonAsync<List<TenantDomain>>() ?? [];
    }

    public Task AddDomainAsync(Guid tenantId, string name) =>
        SendAsync(HttpMethod.Post, $"api/tenant/{tenantId}/domain", new { name });

    // Fails with the auth server's message if the TXT record is not in the domain's DNS yet.
    public Task VerifyDomainAsync(Guid tenantId, Guid domainId) =>
        SendAsync(HttpMethod.Post, $"api/tenant/{tenantId}/domain/{domainId}/verify");

    public Task DeleteDomainAsync(Guid tenantId, Guid domainId) =>
        SendAsync(HttpMethod.Delete, $"api/tenant/{tenantId}/domain/{domainId}");

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

        var response = await http.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return response;

        // Shows the auth server's error message.
        var text = await response.Content.ReadAsStringAsync();
        throw new AuthServerApiException(ReadErrorMessage(text) ?? $"{(int)response.StatusCode} {text}");
    }

    // Gets a new token for each call.
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

// A domain of the tenant in the auth server.
public record TenantDomain(Guid Id, string Name, string TxtRecord, DateTimeOffset? VerifiedAt);
