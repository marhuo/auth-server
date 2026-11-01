using AuthServer.Domain.Common;
using AuthServer.Domain.OAuth;

namespace AuthServer.Application.OAuth.Services;

// Authorization codes of the authorization code flow (with PKCE).
public class AuthorizationCodeService(IAuthorizationCodeRepository codes, TimeProvider timeProvider)
{
    // Stores a new code and returns the raw code for the redirect to the client.
    public async Task<string> CreateAsync(
        Guid clientId,
        Guid userId,
        Guid tenantId,
        string redirectUri,
        IEnumerable<string> scopes,
        string codeChallenge,
        string? nonce)
    {
        var (code, rawCode) = AuthorizationCode.Create(
            clientId: clientId,
            userId: userId,
            tenantId: tenantId,
            redirectUri: redirectUri,
            scopes: scopes,
            codeChallenge: codeChallenge,
            nonce: nonce,
            now: timeProvider.GetUtcNow());

        await codes.CreateAsync(code);
        return rawCode;
    }

    // Exchanges the code once. Returns null if the code is unknown, expired, already used,
    // issued to another client or redirect URI, or the PKCE verifier does not match.
    // Documented exception: returns a domain object (see CLAUDE.md).
    public async Task<AuthorizationCode?> RedeemAsync(string rawCode, Guid clientId, string redirectUri, string codeVerifier)
    {
        var now = timeProvider.GetUtcNow();
        var code = await codes.GetByHashAsync(OpaqueToken.Hash(rawCode));

        if (code is null || !code.CanBeExchangedForAccessToken(clientId, redirectUri, codeVerifier, now))
            return null;

        // Another request may have used the code after it was read.
        return await codes.TryMarkUsedAsync(code.Id, now) ? code : null;
    }
}
