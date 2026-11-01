using System.Text.Json.Serialization;

namespace AuthServer.Domain.OAuth;

// A rotating refresh token. Every refresh exchanges the token for a new one in the same family;
// reuse of an already exchanged token revokes the whole family. Only the hash is stored.
public sealed class RefreshToken : AggregateRoot
{
    private readonly List<string> _scopes;

    [JsonConstructor]
    public RefreshToken(
        Guid id,
        string tokenHash,
        Guid familyId,
        Guid clientId,
        Guid userId,
        Guid tenantId,
        IReadOnlyList<string> scopes,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        DateTimeOffset? exchangedAt,
        DateTimeOffset? revokedAt)
        : base(id)
    {
        TokenHash = tokenHash;
        FamilyId = familyId;
        ClientId = clientId;
        UserId = userId;
        TenantId = tenantId;
        _scopes = [.. scopes];
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        ExchangedAt = exchangedAt;
        RevokedAt = revokedAt;
    }

    public string TokenHash { get; private set; }

    // Shared by all tokens rotated from one sign-in.
    public Guid FamilyId { get; private set; }

    // Id of the client (not the OAuth client_id string).
    public Guid ClientId { get; private set; }

    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }

    // Scopes granted at sign-in. A refresh may request the same or fewer.
    public IReadOnlyList<string> Scopes => _scopes;

    public DateTimeOffset CreatedAt { get; private set; }

    // The token is not valid at or after this time.
    public DateTimeOffset ExpiresAt { get; private set; }

    // When the token was exchanged for a new one.
    public DateTimeOffset? ExchangedAt { get; private set; }

    // When the token was blocked before expiry.
    public DateTimeOffset? RevokedAt { get; private set; }

    [JsonIgnore]
    public bool IsExchanged => ExchangedAt is not null;

    // Returns the aggregate and the raw token that is given to the client.
    // A null familyId starts a new family (a new sign-in).
    public static (RefreshToken Token, string RawToken) Create(
        Guid clientId,
        Guid userId,
        Guid tenantId,
        IEnumerable<string> scopes,
        Guid? familyId,
        TimeSpan lifetime,
        DateTimeOffset now)
    {
        var rawToken = OpaqueToken.Generate();

        var token = new RefreshToken(
            id: Guid.CreateVersion7(),
            tokenHash: OpaqueToken.Hash(rawToken),
            familyId: familyId ?? Guid.CreateVersion7(),
            clientId: clientId,
            userId: userId,
            tenantId: tenantId,
            scopes: [.. scopes],
            createdAt: now,
            expiresAt: now + lifetime,
            exchangedAt: null,
            revokedAt: null);

        return (token, rawToken);
    }

    // Not revoked or expired, and the requested scopes are a subset of the granted ones.
    // Reuse (IsExchanged) is checked separately because it revokes the family.
    public bool CanBeRenewed(IReadOnlyCollection<string> requestedScopes, DateTimeOffset now) =>
        RevokedAt is null
        && ExpiresAt > now
        && !requestedScopes.Except(_scopes).Any();
}
