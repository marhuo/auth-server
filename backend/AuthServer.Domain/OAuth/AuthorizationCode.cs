using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace AuthServer.Domain.OAuth;

// A one-time code of the authorization code flow (with PKCE). Only the hash of the code is stored.
public sealed class AuthorizationCode : AggregateRoot
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);

    private readonly List<string> _scopes;

    [JsonConstructor]
    public AuthorizationCode(
        Guid id,
        string codeHash,
        Guid clientId,
        Guid userId,
        Guid tenantId,
        string redirectUri,
        IReadOnlyList<string> scopes,
        string codeChallenge,
        string? nonce,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        DateTimeOffset? usedAt)
        : base(id)
    {
        CodeHash = codeHash;
        ClientId = clientId;
        UserId = userId;
        TenantId = tenantId;
        RedirectUri = redirectUri;
        _scopes = [.. scopes];
        CodeChallenge = codeChallenge;
        Nonce = nonce;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        UsedAt = usedAt;
    }

    public string CodeHash { get; private set; }

    // Id of the client (not the OAuth client_id string).
    public Guid ClientId { get; private set; }

    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public string RedirectUri { get; private set; }
    public IReadOnlyList<string> Scopes => _scopes;

    // S256 PKCE challenge: base64url(SHA-256(code_verifier)).
    public string CodeChallenge { get; private set; }

    public string? Nonce { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    // Returns the aggregate and the raw code that is given to the client.
    public static (AuthorizationCode Code, string RawCode) Create(
        Guid clientId,
        Guid userId,
        Guid tenantId,
        string redirectUri,
        IEnumerable<string> scopes,
        string codeChallenge,
        string? nonce,
        DateTimeOffset now)
    {
        var rawCode = OpaqueToken.Generate();

        var code = new AuthorizationCode(
            id: Guid.CreateVersion7(),
            codeHash: OpaqueToken.Hash(rawCode),
            clientId: clientId,
            userId: userId,
            tenantId: tenantId,
            redirectUri: redirectUri,
            scopes: [.. scopes],
            codeChallenge: codeChallenge,
            nonce: nonce,
            createdAt: now,
            expiresAt: now + Lifetime,
            usedAt: null);

        return (code, rawCode);
    }

    // Whether the token endpoint may exchange the code for an access token (and, depending on
    // the scopes, an ID token and a refresh token). The code can be exchanged only once, by the
    // same client, with the same redirect URI, before it expires, and with a verifier that
    // matches the PKCE challenge.
    public bool CanBeExchangedForAccessToken(Guid clientId, string redirectUri, string codeVerifier, DateTimeOffset now) =>
        ClientId == clientId
        && UsedAt is null
        && ExpiresAt > now
        && string.Equals(RedirectUri, redirectUri, StringComparison.Ordinal)
        && PkceMatches(codeVerifier);

    private bool PkceMatches(string codeVerifier)
    {
        var computed = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computed),
            Encoding.ASCII.GetBytes(CodeChallenge));
    }
}
