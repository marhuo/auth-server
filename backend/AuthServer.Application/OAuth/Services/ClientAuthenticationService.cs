using AuthServer.Domain.Clients;

namespace AuthServer.Application.OAuth.Services;

public enum ClientAuthStatus { Success, Unknown, InvalidSecret }

// Client lookups for the OAuth endpoints. Returns the domain Client because the endpoints
// need its behavior (allowed redirect URIs, grant types, scopes).
// Documented exception: returns a domain object (see CLAUDE.md).
public class ClientAuthenticationService(IClientRepository clients)
{
    public async Task<(ClientAuthStatus Status, Client? Client)> AuthenticateAsync(string? clientId, string? secret)
    {
        if (string.IsNullOrEmpty(clientId))
            return (ClientAuthStatus.Unknown, null);

        var client = await clients.FindActiveByClientIdAsync(clientId);
        if (client is null)
            return (ClientAuthStatus.Unknown, null);

        if (client.IsConfidential && !client.VerifySecret(secret))
            return (ClientAuthStatus.InvalidSecret, null);

        return (ClientAuthStatus.Success, client);
    }

    public Task<Client?> FindActiveAsync(string clientId) =>
        clients.FindActiveByClientIdAsync(clientId);
}
