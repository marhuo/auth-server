using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AuthServer.Domain.Clients;

public sealed partial class Client : AggregateRoot
{
    public const int MinClientIdLength = 2;
    public const int MaxClientIdLength = 100;
    public const int MaxDisplayNameLength = 200;
    public const int MinSecretLength = 10;

    // Default token lifetimes for new clients. ID and refresh token defaults apply only to clients
    // that sign in users.
    public static readonly TimeSpan DefaultAccessTokenLifetime = TimeSpan.FromMinutes(60);
    public static readonly TimeSpan DefaultIdTokenLifetime = TimeSpan.FromMinutes(60);
    public static readonly TimeSpan DefaultRefreshTokenLifetime = TimeSpan.FromDays(30);

    // Error keys that are not property names of the aggregate; they match the request fields.
    private const string SecretKey = "secret";
    private const string NewSecretKey = "newSecret";
    private const string UriKey = "uri";
    private const string ScopeIdKey = "scopeId";

    private readonly List<string> _redirectUris;
    private readonly List<string> _postLogoutRedirectUris;
    private readonly List<Guid> _scopeIds;

    [JsonConstructor]
    public Client(
        Guid id,
        ClientTypes type,
        string clientId,
        string displayName,
        string? secretHash,
        bool isActive,
        Guid targetResourceServerId,
        Guid? tenantId,
        IReadOnlyList<string> redirectUris,
        IReadOnlyList<string> postLogoutRedirectUris,
        string? initiateLoginUri,
        IReadOnlyList<Guid> scopeIds,
        TimeSpan accessTokenLifetime,
        TimeSpan? idTokenLifetime,
        TimeSpan? refreshTokenLifetime,
        DateTimeOffset createdAt)
        : base(id)
    {
        AccessTokenLifetime = accessTokenLifetime;
        IdTokenLifetime = idTokenLifetime;
        RefreshTokenLifetime = refreshTokenLifetime;
        Type = type;
        ClientId = clientId;
        DisplayName = displayName;
        SecretHash = secretHash;
        IsActive = isActive;
        TargetResourceServerId = targetResourceServerId;
        TenantId = tenantId;
        _redirectUris = [.. redirectUris];
        _postLogoutRedirectUris = [.. postLogoutRedirectUris];
        InitiateLoginUri = initiateLoginUri;
        _scopeIds = [.. scopeIds];
        CreatedAt = createdAt;
    }

    public ClientTypes Type { get; private set; }
    public string ClientId { get; private set; }
    public string DisplayName { get; private set; }

    // SHA-256 of the secret as hex. Null for public clients.
    public string? SecretHash { get; private set; }

    public bool IsActive { get; private set; }

    // The resource server the client connects to: the product users sign in to through the client, or
    // the API it calls (the auth server for a product's backend client). The client may have only
    // this resource server's scopes (plus global scopes), so its access tokens have one audience.
    public Guid TargetResourceServerId { get; private set; }

    // Set when the client belongs to a specific tenant. Required for machine clients.
    public Guid? TenantId { get; private set; }

    public IReadOnlyList<string> RedirectUris => _redirectUris;
    public IReadOnlyList<string> PostLogoutRedirectUris => _postLogoutRedirectUris;

    // The product's address that starts the sign-in (e.g. https://app.example.com/Login), like Okta's
    // "Initiate login URI". The auth server cannot start a sign-in for a product itself, so an
    // invitation email links to this address. Only for clients that
    // sign in users; null when not set.
    public string? InitiateLoginUri { get; private set; }
    public IReadOnlyList<Guid> ScopeIds => _scopeIds;
    public DateTimeOffset CreatedAt { get; private set; }

    [JsonIgnore]
    public bool IsConfidential => Type.IsConfidential;

    [JsonIgnore]
    public bool SignsInUsers => Type.SignsInUsers;

    [JsonIgnore]
    public IReadOnlyList<string> GrantTypes => ClientGrants.For(Type);

    public TimeSpan AccessTokenLifetime { get; private set; }

    // ID and refresh token lifetimes are null for clients that do not sign in users (machine clients).
    public TimeSpan? IdTokenLifetime { get; private set; }
    public TimeSpan? RefreshTokenLifetime { get; private set; }

    public static Client Create(
        ClientTypes type,
        string clientId,
        string displayName,
        string? secret,
        Guid targetResourceServerId,
        Guid? tenantId,
        IEnumerable<string>? redirectUris,
        IEnumerable<string>? postLogoutRedirectUris,
        IEnumerable<Guid>? scopeIds,
        TimeSpan accessTokenLifetime,
        TimeSpan? idTokenLifetime,
        TimeSpan? refreshTokenLifetime,
        DateTimeOffset now)
    {
        clientId = clientId.Trim();
        displayName = displayName.Trim();
        var redirects = CleanUris(redirectUris);
        var postLogouts = CleanUris(postLogoutRedirectUris);
        var scopes = (scopeIds ?? []).Distinct().ToList();

        var errors = new ErrorCollector();
        ValidateCreate(errors, type, clientId, displayName, secret, redirects, postLogouts, scopes);
        ValidateTokenLifetimes(errors, type, accessTokenLifetime, idTokenLifetime, refreshTokenLifetime);
        errors.ThrowIfAny();

        return new Client(
            id: Guid.CreateVersion7(),
            type: type,
            clientId: clientId,
            displayName: displayName,
            secretHash: secret is null ? null : HashSecret(secret),
            isActive: true,
            targetResourceServerId: targetResourceServerId,
            tenantId: tenantId,
            redirectUris: redirects,
            postLogoutRedirectUris: postLogouts,
            initiateLoginUri: null,
            scopeIds: scopes,
            accessTokenLifetime: accessTokenLifetime,
            idTokenLifetime: idTokenLifetime,
            refreshTokenLifetime: refreshTokenLifetime,
            createdAt: now);
    }

    public void SetClientId(string clientId)
    {
        clientId = clientId.Trim();

        var errors = new ErrorCollector();
        ValidateClientId(errors, clientId);
        errors.ThrowIfAny();

        ClientId = clientId;
    }

    public void SetDisplayName(string displayName)
    {
        displayName = displayName.Trim();

        var errors = new ErrorCollector();
        ValidateDisplayName(errors, displayName);
        errors.ThrowIfAny();

        DisplayName = displayName;
    }

    public void SetTenantId(Guid? tenantId)
    {

        var errors = new ErrorCollector();
        ValidateTenantId(errors, tenantId);
        errors.ThrowIfAny();

        TenantId = tenantId;
    }

    // A blank value clears the address.
    public void SetInitiateLoginUri(string? uri)
    {
        uri = string.IsNullOrWhiteSpace(uri) ? null : uri.Trim();

        var errors = new ErrorCollector();
        ValidateInitiateLoginUri(errors, Type, uri);
        errors.ThrowIfAny();

        InitiateLoginUri = uri;
    }

    public void ChangeSecret(string newSecret)
    {

        var errors = new ErrorCollector();
        ValidateChangeSecret(errors, newSecret);
        errors.ThrowIfAny();

        SecretHash = HashSecret(newSecret);
    }

    // Whether the scope belongs to the client's resource server is checked by ClientValidator.
    public void AddScope(Guid scopeId)
    {

        if (_scopeIds.Contains(scopeId))
            return;

        _scopeIds.Add(scopeId);
    }

    public void RemoveScope(Guid scopeId)
    {

        var errors = new ErrorCollector();
        ValidateRemoveScope(errors, scopeId);
        errors.ThrowIfAny();

        _scopeIds.Remove(scopeId);
    }

    public void AddRedirectUri(string uri)
    {
        uri = uri.Trim();

        var errors = new ErrorCollector();
        ValidateAddRedirectUri(errors, uri);
        errors.ThrowIfAny();

        _redirectUris.Add(uri);
    }

    public void RemoveRedirectUri(string uri)
    {
        uri = uri.Trim();

        var errors = new ErrorCollector();
        ValidateRemoveRedirectUri(errors, uri);
        errors.ThrowIfAny();

        _redirectUris.Remove(uri);
    }

    public void AddPostLogoutRedirectUri(string uri)
    {
        uri = uri.Trim();

        var errors = new ErrorCollector();
        ValidateAddPostLogoutRedirectUri(errors, uri);
        errors.ThrowIfAny();

        _postLogoutRedirectUris.Add(uri);
    }

    public void RemovePostLogoutRedirectUri(string uri)
    {
        _postLogoutRedirectUris.Remove(uri.Trim());
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public bool VerifySecret(string? secret)
    {
        if (SecretHash is null || string.IsNullOrEmpty(secret))
            return false;

        var computed = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        var stored = Convert.FromHexString(SecretHash);

        return CryptographicOperations.FixedTimeEquals(computed, stored);
    }

    public bool IsGrantTypeAllowed(string? grantType) =>
        !string.IsNullOrEmpty(grantType) && GrantTypes.Contains(grantType);

    public bool IsRedirectUriAllowed(string? uri) =>
        !string.IsNullOrEmpty(uri) && _redirectUris.Contains(uri, StringComparer.Ordinal);

    public bool IsPostLogoutRedirectUriAllowed(string? uri) =>
        !string.IsNullOrEmpty(uri) && _postLogoutRedirectUris.Contains(uri, StringComparer.Ordinal);

    public bool AreScopesAllowed(IEnumerable<Guid> scopeIds) => scopeIds.All(_scopeIds.Contains);

    // ---- Validation. Used by the aggregate and by ClientValidator. Values must be trimmed/cleaned. ----

    public static void ValidateCreate(
        ErrorCollector errors,
        ClientTypes type,
        string clientId,
        string displayName,
        string? secret,
        IReadOnlyCollection<string> redirectUris,
        IReadOnlyCollection<string> postLogoutRedirectUris,
        IReadOnlyCollection<Guid> scopeIds)
    {
        ValidateClientId(errors, clientId);
        ValidateDisplayName(errors, displayName);

        if (type.IsConfidential && (secret is null || secret.Length < MinSecretLength))
            errors.Add(SecretKey, ErrorCode.InvalidLength,
                $"Secret must be at least {MinSecretLength} characters.",
                $"Salaisuuden on oltava vähintään {MinSecretLength} merkkiä pitkä.");

        if (!type.IsConfidential && secret is not null)
            errors.Add(SecretKey, ErrorCode.NotAllowed,
                "A public client cannot have a secret.",
                "Julkisella clientilla ei voi olla salaisuutta.");

        if (type.SignsInUsers && redirectUris.Count == 0)
            errors.Add(nameof(RedirectUris), ErrorCode.Required,
                "At least one redirect URI is required.",
                "Vähintään yksi redirect-osoite vaaditaan.");

        if (type == ClientTypes.ConfidentialMachine)
        {
            if (scopeIds.Count == 0)
                errors.Add(nameof(ScopeIds), ErrorCode.Required,
                    "A machine client needs at least one scope.",
                    "Konetunnus tarvitsee vähintään yhden scopen.");

            if (redirectUris.Count > 0 || postLogoutRedirectUris.Count > 0)
                errors.Add(nameof(RedirectUris), ErrorCode.NotAllowed,
                    "A machine client cannot have redirect URIs.",
                    "Konetunnuksella ei voi olla redirect-osoitteita.");
        }

        if (redirectUris.Concat(postLogoutRedirectUris).Any(uri => !IsValidRedirectUri(uri)))
            errors.Add(nameof(RedirectUris), ErrorCode.InvalidFormat,
                "A redirect URI is not valid. Use https, or http for localhost only.",
                "Redirect-osoite ei kelpaa. Käytä https-osoitetta, tai http vain localhostille.");
    }

    // Clients that sign in users need ID and refresh token lifetimes; machine clients must not have them.
    public static void ValidateTokenLifetimes(
        ErrorCollector errors,
        ClientTypes type,
        TimeSpan accessTokenLifetime,
        TimeSpan? idTokenLifetime,
        TimeSpan? refreshTokenLifetime)
    {
        if (accessTokenLifetime <= TimeSpan.Zero)
            errors.Add(nameof(AccessTokenLifetime), ErrorCode.OutOfRange,
                "Access token lifetime must be positive.",
                "Access tokenin voimassaoloajan on oltava positiivinen.");

        ValidateUserTokenLifetime(errors, type, nameof(IdTokenLifetime), "ID token", idTokenLifetime);
        ValidateUserTokenLifetime(errors, type, nameof(RefreshTokenLifetime), "Refresh token", refreshTokenLifetime);
    }

    private static void ValidateUserTokenLifetime(
        ErrorCollector errors, ClientTypes type, string key, string tokenName, TimeSpan? lifetime)
    {
        if (type.SignsInUsers && lifetime is null)
            errors.Add(key, ErrorCode.Required,
                $"{tokenName} lifetime is required.",
                $"{tokenName}in voimassaoloaika on pakollinen.");

        if (type.SignsInUsers && lifetime <= TimeSpan.Zero)
            errors.Add(key, ErrorCode.OutOfRange,
                $"{tokenName} lifetime must be positive.",
                $"{tokenName}in voimassaoloajan on oltava positiivinen.");

        if (!type.SignsInUsers && lifetime is not null)
            errors.Add(key, ErrorCode.NotAllowed,
                $"{tokenName} lifetime is not allowed for a machine client.",
                $"Konetunnuksella ei voi olla {tokenName}in voimassaoloaikaa.");
    }

    public static void ValidateClientId(ErrorCollector errors, string clientId)
    {
        if (clientId.Length is < MinClientIdLength or > MaxClientIdLength || !ClientIdRegex().IsMatch(clientId))
            errors.Add(nameof(ClientId), ErrorCode.InvalidFormat,
                $"Client id may contain only letters, digits, '.', '_' and '-' ({MinClientIdLength}-{MaxClientIdLength} characters).",
                $"Client id saa sisältää vain kirjaimia, numeroita sekä merkit '.', '_' ja '-' ({MinClientIdLength}–{MaxClientIdLength} merkkiä).");
    }

    public static void ValidateDisplayName(ErrorCollector errors, string displayName)
    {
        if (displayName.Length is 0 or > MaxDisplayNameLength)
            errors.Add(nameof(DisplayName), ErrorCode.InvalidLength,
                $"Display name must be 1-{MaxDisplayNameLength} characters.",
                $"Näyttönimen on oltava 1–{MaxDisplayNameLength} merkkiä pitkä.");
    }

    public void ValidateTenantId(ErrorCollector errors, Guid? tenantId)
    {
        if (Type == ClientTypes.ConfidentialMachine && tenantId is null)
            errors.Add(nameof(TenantId), ErrorCode.Required,
                "A machine client must belong to a tenant.",
                "Konetunnuksen on kuuluttava asiakkaalle.");
    }

    public void ValidateChangeSecret(ErrorCollector errors, string newSecret)
    {
        if (!IsConfidential)
        {
            errors.Add("id", ErrorCode.NotAllowed,
                "A public client cannot have a secret.",
                "Julkisella clientilla ei voi olla salaisuutta.");
            return;
        }

        if (newSecret.Length < MinSecretLength)
            errors.Add(NewSecretKey, ErrorCode.InvalidLength,
                $"Secret must be at least {MinSecretLength} characters.",
                $"Salaisuuden on oltava vähintään {MinSecretLength} merkkiä pitkä.");
    }

    public void ValidateRemoveScope(ErrorCollector errors, Guid scopeId)
    {
        if (_scopeIds.Contains(scopeId) && Type == ClientTypes.ConfidentialMachine && _scopeIds.Count == 1)
            errors.Add(ScopeIdKey, ErrorCode.NotAllowed,
                "A machine client needs at least one scope.",
                "Konetunnus tarvitsee vähintään yhden scopen.");
    }

    public void ValidateAddRedirectUri(ErrorCollector errors, string uri) =>
        ValidateAddUri(errors, uri, _redirectUris);

    public void ValidateAddPostLogoutRedirectUri(ErrorCollector errors, string uri) =>
        ValidateAddUri(errors, uri, _postLogoutRedirectUris);

    public void ValidateRemoveRedirectUri(ErrorCollector errors, string uri)
    {
        if (_redirectUris.Contains(uri) && SignsInUsers && _redirectUris.Count == 1)
            errors.Add(UriKey, ErrorCode.NotAllowed,
                "At least one redirect URI is required.",
                "Vähintään yksi redirect-osoite vaaditaan.");
    }

    // Expects a trimmed value or null.
    public static void ValidateInitiateLoginUri(ErrorCollector errors, ClientTypes type, string? uri)
    {
        if (uri is null)
            return;

        if (!type.SignsInUsers)
        {
            errors.Add(nameof(InitiateLoginUri), ErrorCode.NotAllowed,
                "A machine client cannot have an initiate login URI.",
                "Konetunnuksella ei voi olla kirjautumisen aloitusosoitetta.");
            return;
        }

        if (!IsValidRedirectUri(uri))
            errors.Add(nameof(InitiateLoginUri), ErrorCode.InvalidFormat,
                "The URI is not valid. Use https, or http for localhost only.",
                "Osoite ei kelpaa. Käytä https-osoitetta, tai http vain localhostille.");
    }

    public static ValidationError ClientIdTakenError() =>
        new(nameof(ClientId), ErrorCode.Taken, "Client id is taken.", "Client id on jo käytössä.");

    // Trims, drops empty values and duplicates.
    public static List<string> CleanUris(IEnumerable<string>? values) =>
        (values ?? [])
            .Select(v => v?.Trim() ?? "")
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private void ValidateAddUri(ErrorCollector errors, string uri, List<string> existing)
    {
        if (!SignsInUsers)
        {
            errors.Add(UriKey, ErrorCode.NotAllowed,
                "A machine client cannot have redirect URIs.",
                "Konetunnuksella ei voi olla redirect-osoitteita.");
            return;
        }

        if (!IsValidRedirectUri(uri))
        {
            errors.Add(UriKey, ErrorCode.InvalidFormat,
                "The URI is not valid. Use https, or http for localhost only.",
                "Osoite ei kelpaa. Käytä https-osoitetta, tai http vain localhostille.");
            return;
        }

        if (existing.Contains(uri))
            errors.Add(UriKey, ErrorCode.Taken,
                "The URI has already been added.",
                "Osoite on jo lisätty.");
    }

    private static bool IsValidRedirectUri(string value)
    {
        if (value.Contains('*') || !Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return false;

        if (!string.IsNullOrEmpty(uri.Fragment))
            return false;

        return uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback);
    }

    private static string HashSecret(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    [GeneratedRegex(@"^[a-zA-Z0-9][a-zA-Z0-9._-]*\z")]
    private static partial Regex ClientIdRegex();
}
