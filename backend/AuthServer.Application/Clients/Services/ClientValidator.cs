using AuthServer.Application.Clients.Messages;
using AuthServer.Domain.Clients;
using AuthServer.Domain.Common;
using AuthServer.Domain.ResourceServers;
using AuthServer.Domain.Scopes;
using AuthServer.Domain.Tenants;

namespace AuthServer.Application.Clients.Services;

public class ClientValidator(
    IClientRepository clients,
    IResourceServerRepository resourceServers,
    ITenantRepository tenants,
    IScopeRepository scopes)
{
    public async Task ValidateCreateAsync(ClientCreateRequest request)
    {
        var type = request.Type.ToDomain();
        var clientId = request.ClientId.Trim();
        var scopeIds = request.ScopeIds.Distinct().ToList();

        var errors = new ErrorCollector();
        Client.ValidateCreate(
            errors, type, clientId, request.DisplayName.Trim(), request.Secret,
            Client.CleanUris(request.RedirectUris), Client.CleanUris(request.PostLogoutRedirectUris), scopeIds);
        errors.ThrowIfAny();

        if (!await resourceServers.ExistsAsync(request.ResourceServerId))
            throw new ValidationException(
                Errors.NotFound(nameof(request.ResourceServerId), "Resource server", "Resurssipalvelinta"));

        await ValidateTenantAsync(request.TenantId);
        await ValidateScopesAsync(type, request.ResourceServerId, scopeIds, nameof(request.ScopeIds));

        if (await clients.ClientIdExistsAsync(clientId))
            throw new ValidationException(Client.ClientIdTakenError());
    }

    public async Task ValidateUpdateAsync(Client client, ClientUpdateRequest request)
    {
        var clientId = request.ClientId.Trim();

        var errors = new ErrorCollector();
        Client.ValidateClientId(errors, clientId);
        Client.ValidateDisplayName(errors, request.DisplayName.Trim());
        client.ValidateTenantId(errors, request.TenantId);
        errors.ThrowIfAny();

        await ValidateTenantAsync(request.TenantId);

        if (clientId != client.ClientId
            && await clients.ClientIdExistsAsync(clientId, excludeId: client.Id))
            throw new ValidationException(Client.ClientIdTakenError());
    }

    public void ValidateChangeSecret(Client client, ClientChangeSecretRequest request) =>
        Validate(errors => client.ValidateChangeSecret(errors, request.NewSecret));

    public Task ValidateAddScopeAsync(Client client, ClientAddScopeRequest request) =>
        ValidateScopesAsync(client.Type, client.ResourceServerId, [request.ScopeId], nameof(request.ScopeId));

    public void ValidateRemoveScope(Client client, ClientRemoveScopeRequest request) =>
        Validate(errors => client.ValidateRemoveScope(errors, request.ScopeId));

    public void ValidateAddRedirectUri(Client client, ClientAddRedirectUriRequest request) =>
        Validate(errors => client.ValidateAddRedirectUri(errors, request.Uri.Trim()));

    public void ValidateRemoveRedirectUri(Client client, ClientRemoveRedirectUriRequest request) =>
        Validate(errors => client.ValidateRemoveRedirectUri(errors, request.Uri.Trim()));

    public void ValidateAddPostLogoutRedirectUri(Client client, ClientAddPostLogoutRedirectUriRequest request) =>
        Validate(errors => client.ValidateAddPostLogoutRedirectUri(errors, request.Uri.Trim()));

    private static void Validate(Action<ErrorCollector> rules)
    {
        var errors = new ErrorCollector();
        rules(errors);
        errors.ThrowIfAny();
    }

    private async Task ValidateTenantAsync(Guid? tenantId)
    {
        if (tenantId is { } id && !await tenants.ExistsAsync(id))
            throw new ValidationException(Errors.NotFound("tenantId", "Tenant", "Asiakasta"));
    }

    // Every scope must exist and belong to the client's resource server, or be global (no resource server).
    // Machine clients cannot have global scopes.
    private async Task ValidateScopesAsync(ClientTypes type, Guid resourceServerId, IReadOnlyCollection<Guid> scopeIds, string key)
    {
        if (scopeIds.Count == 0)
            return;

        var found = await scopes.GetByIdsAsync(scopeIds);

        if (found.Count != scopeIds.Distinct().Count())
            throw new ValidationException(Errors.NotFound(key, "Scope", "Scopea"));

        if (!type.SignsInUsers && found.Any(s => s.ResourceServerId is null))
            throw new ValidationException(key, ErrorCode.NotAllowed,
                "A machine client cannot have global scopes.",
                "Konetunnuksella ei voi olla yleisiä scopeja.");

        if (found.Any(s => s.ResourceServerId is { } owner && owner != resourceServerId))
            throw new ValidationException(key, ErrorCode.NotAllowed,
                "A client can only have scopes of its own resource server or global scopes.",
                "Clientillä voi olla vain oman resurssipalvelimensa scopeja tai yleisiä scopeja.");
    }
}
