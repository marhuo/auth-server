namespace AuthServer.Domain.OAuth;

// Contents of a JWT to sign. TokenType is the "typ" header (e.g. "at+jwt"); null uses the default.
public sealed record JwtContent(
    string Issuer,
    string? Audience,
    string? TokenType,
    IReadOnlyDictionary<string, object> Claims,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);

// Signs JWTs with the active signing key (RS256).
public interface ITokenSigner
{
    Task<string> SignAsync(JwtContent content);
}
