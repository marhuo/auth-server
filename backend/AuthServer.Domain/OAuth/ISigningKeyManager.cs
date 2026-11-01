namespace AuthServer.Domain.OAuth;

// Manages the RSA keys that sign the tokens.
public interface ISigningKeyManager
{
    // Creates an active key if there is none. Called at startup.
    Task EnsureActiveKeyAsync();

    // Retires the active key and creates a new one. Retired keys stay published for a while
    // so that tokens signed with them can still be verified.
    Task RotateAsync();

    // Public keys of the active and recently retired keys as JWKs (RFC 7517).
    Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> GetPublicJwksAsync();
}
