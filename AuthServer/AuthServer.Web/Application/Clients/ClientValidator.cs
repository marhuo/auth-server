using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AuthServer.Web.Application.Clients;

public partial class ClientValidator(AuthDbContext db)
{
    private const int MinSecretLength = 10;
    private const int MaxDisplayNameLength = 200;

    [GeneratedRegex(@"^[a-zA-Z0-9][a-zA-Z0-9._-]{1,99}\z")]
    private static partial Regex ClientIdRegex();

    public async Task ValidateCreateAsync(
        ClientTypes type,
        string clientId,
        string displayName,
        string? secret,
        Guid resourceServerId,
        Guid? tenantId,
        IReadOnlyCollection<string> redirectUris,
        IReadOnlyCollection<string> postLogoutRedirectUris,
        IReadOnlyCollection<Guid> scopeIds)
    {
        var needsSecret = type.IsConfidential;
        var signsInUsers = type.SignsInUsers;

        var errors = new ErrorCollector();
        ValidateClientId(errors, clientId);
        ValidateDisplayName(errors, displayName);

        if (needsSecret && (secret is null || secret.Length < MinSecretLength))
            errors.Add("secret", ErrorCode.InvalidLength,
                $"Secret must be at least {MinSecretLength} characters.",
                $"Salaisuuden on oltava vähintään {MinSecretLength} merkkiä pitkä.");

        if (!needsSecret && secret is not null)
            errors.Add("secret", ErrorCode.NotAllowed,
                "A public client cannot have a secret.",
                "Julkisella clientilla ei voi olla salaisuutta.");

        if (signsInUsers && redirectUris.Count == 0)
            errors.Add("redirectUris", ErrorCode.Required,
                "At least one redirect URI is required.",
                "Vähintään yksi redirect-osoite vaaditaan.");

        if (type == ClientTypes.ConfidentialMachine)
        {
            if (scopeIds.Count == 0)
                errors.Add("scopeIds", ErrorCode.Required,
                    "A machine client needs at least one scope.",
                    "Konetunnus tarvitsee vähintään yhden scopen.");

            if (redirectUris.Count > 0 || postLogoutRedirectUris.Count > 0)
                errors.Add("redirectUris", ErrorCode.NotAllowed,
                    "A machine client cannot have redirect URIs.",
                    "Konetunnuksella ei voi olla redirect-osoitteita.");
        }

        if (redirectUris.Concat(postLogoutRedirectUris).Any(uri => !IsValidRedirectUri(uri)))
            errors.Add("redirectUris", ErrorCode.InvalidFormat,
                "A redirect URI is not valid. Use https, or http for localhost only.",
                "Redirect-osoite ei kelpaa. Käytä https-osoitetta, tai http vain localhostille.");

        errors.ThrowIfAny();

        await ValidateResourceServerAsync(resourceServerId);
        await ValidateTenantAsync(tenantId);
        await ValidateScopesAsync(type, resourceServerId, scopeIds, "scopeIds");

        if (await db.Clients.AnyAsync(c => c.ClientId == clientId))
            throw ClientIdTaken();
    }

    // client = the current state in the database.
    public async Task ValidateUpdateAsync(ClientEntity client, string clientId, string displayName, Guid? tenantId)
    {
        var errors = new ErrorCollector();
        ValidateClientId(errors, clientId);
        ValidateDisplayName(errors, displayName);

        if (client.Type == ClientTypes.ConfidentialMachine && tenantId is null)
            errors.Add("tenantId", ErrorCode.Required,
                "A machine client must belong to a tenant.",
                "Konetunnuksen on kuuluttava asiakkaalle.");

        errors.ThrowIfAny();

        await ValidateTenantAsync(tenantId);

        if (clientId != client.ClientId
            && await db.Clients.AnyAsync(c => c.ClientId == clientId && c.Id != client.Id))
            throw ClientIdTaken();
    }

    public void ValidateChangeSecret(ClientEntity client, string newSecret)
    {
        if (!client.Type.IsConfidential)
            throw NotAllowed("id",
                "A public client cannot have a secret.",
                "Julkisella clientilla ei voi olla salaisuutta.");

        if (newSecret.Length < MinSecretLength)
            throw new ValidationException("newSecret", ErrorCode.InvalidLength,
                $"Secret must be at least {MinSecretLength} characters.",
                $"Salaisuuden on oltava vähintään {MinSecretLength} merkkiä pitkä.");
    }

    // The client must be loaded together with its Scopes.
    public Task ValidateAddScopeAsync(ClientEntity client, Guid scopeId) =>
        ValidateScopesAsync(client.Type, client.ResourceServerId, [scopeId], "scopeId");

    // The client must be loaded together with its Scopes.
    public void ValidateRemoveScope(ClientEntity client)
    {
        if (client.Type == ClientTypes.ConfidentialMachine && client.Scopes.Count == 1)
            throw NotAllowed("scopeId",
                "A machine client needs at least one scope.",
                "Konetunnus tarvitsee vähintään yhden scopen.");
    }

    public void ValidateAddUri(ClientEntity client, string uri, IReadOnlyCollection<string> existingUris)
    {
        if (!client.Type.SignsInUsers)
            throw NotAllowed("uri",
                "A machine client cannot have redirect URIs.",
                "Konetunnuksella ei voi olla redirect-osoitteita.");

        if (!IsValidRedirectUri(uri))
            throw new ValidationException("uri", ErrorCode.InvalidFormat,
                "The URI is not valid. Use https, or http for localhost only.",
                "Osoite ei kelpaa. Käytä https-osoitetta, tai http vain localhostille.");

        if (existingUris.Contains(uri))
            throw new ValidationException("uri", ErrorCode.Taken,
                "The URI has already been added.",
                "Osoite on jo lisätty.");
    }

    // Call only when the URI exists in the client's redirect URIs.
    public void ValidateRemoveRedirectUri(ClientEntity client)
    {
        if (client.Type.SignsInUsers && client.RedirectUris.Count == 1)
            throw NotAllowed("uri",
                "At least one redirect URI is required.",
                "Vähintään yksi redirect-osoite vaaditaan.");
    }

    public static ValidationException ClientIdTaken() =>
        new("clientId", ErrorCode.Taken, "Client id is taken.", "Client id on jo käytössä.");

    private async Task ValidateResourceServerAsync(Guid resourceServerId)
    {
        if (!await db.ResourceServers.AnyAsync(r => r.Id == resourceServerId))
            throw new ValidationException(
                Errors.NotFound("resourceServerId", "Resource server", "Resurssipalvelinta"));
    }

    private async Task ValidateTenantAsync(Guid? tenantId)
    {
        if (tenantId is { } id && !await db.Tenants.AnyAsync(t => t.Id == id))
            throw new ValidationException(Errors.NotFound("tenantId", "Tenant", "Asiakasta"));
    }

    // Every scope must exist and belong to the resource server, or be global (no resource server).
    private async Task ValidateScopesAsync(ClientTypes type, Guid resourceServerId, IReadOnlyCollection<Guid> scopeIds, string key)
    {
        var ids = scopeIds.Distinct().ToList();

        if (ids.Count == 0)
            return;

        var owners = await db.Scopes
            .Where(s => ids.Contains(s.Id))
            .Select(s => s.ResourceServerId)
            .ToListAsync();

        if (owners.Count != ids.Count)
            throw new ValidationException(Errors.NotFound(key, "Scope", "Scopea"));

        if (!type.SignsInUsers && owners.Any(owner => owner is null))
            throw NotAllowed(key,
                "A machine client cannot have global scopes.",
                "Konetunnuksella ei voi olla yleisiä scopeja.");

        if (owners.Any(owner => owner is not null && owner != resourceServerId))
            throw NotAllowed(key,
                "A client can only have scopes of its own resource server or global scopes.",
                "Clientillä voi olla vain oman resurssipalvelimensa scopeja tai yleisiä scopeja.");
    }

    private static void ValidateClientId(ErrorCollector errors, string clientId)
    {
        if (!ClientIdRegex().IsMatch(clientId))
            errors.Add("clientId", ErrorCode.InvalidFormat,
                "Client id may contain only letters, digits, '.', '_' and '-' (2-100 characters).",
                "Client id saa sisältää vain kirjaimia, numeroita sekä merkit '.', '_' ja '-' (2–100 merkkiä).");
    }

    private static void ValidateDisplayName(ErrorCollector errors, string displayName)
    {
        if (displayName.Length is 0 or > MaxDisplayNameLength)
            errors.Add("displayName", ErrorCode.InvalidLength,
                $"Display name must be 1-{MaxDisplayNameLength} characters.",
                $"Näyttönimen on oltava 1–{MaxDisplayNameLength} merkkiä pitkä.");
    }

    private static bool IsValidRedirectUri(string value)
    {
        if (value.Contains('*') || !Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return false;

        if (!string.IsNullOrEmpty(uri.Fragment))
            return false;

        return uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback);
    }

    private static ValidationException NotAllowed(string key, string en, string fi) =>
        new(key, ErrorCode.NotAllowed, en, fi);
}