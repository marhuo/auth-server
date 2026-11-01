using AuthServer.Domain.Clients;
using AuthServer.Domain.Common;
using AuthServer.Domain.OAuth;
using AuthServer.Domain.Roles;
using AuthServer.Domain.Scopes;
using AuthServer.Domain.Users;
using Microsoft.Extensions.Options;

namespace AuthServer.Application.OAuth.Services;

// Issues access, ID and refresh tokens, and rotates refresh tokens.
public class TokenService(
    IUserRepository users,
    IRoleRepository roleRepository,
    IScopeRepository scopes,
    IRefreshTokenRepository refreshTokens,
    ITokenSigner signer,
    ISigningKeyManager signingKeys,
    IOptions<AuthTokenOptions> options,
    TimeProvider timeProvider)
{
    private AuthTokenOptions Options => options.Value;

    // userId is null for client credentials (the client itself is the subject).
    public async Task<string> CreateAccessTokenAsync(Client client, Guid? userId, Guid? tenantId, IReadOnlyCollection<string> scopeNames)
    {
        var scopeList = scopeNames.ToList();
        var audiences = await scopes.GetAudiencesAsync(scopeList);

        var claims = new Dictionary<string, object>
        {
            ["sub"] = userId?.ToString() ?? client.ClientId,
            ["client_id"] = client.ClientId,
            ["scope"] = string.Join(' ', scopeList),
            ["jti"] = Guid.CreateVersion7().ToString("N")
        };

        if (audiences.Count > 0)
        {
            claims["aud"] = audiences.Count == 1 ? audiences[0] : audiences.ToArray();
        }

        if (tenantId is { } tenant)
        {
            claims["tenant_id"] = tenant.ToString();

            if (userId is { } user)
            {
                var roles = await roleRepository.GetTenantRoleNamesAsync(user, tenant);
                if (roles.Count > 0)
                {
                    claims["role"] = roles.ToArray();
                }
            }
        }

        var now = timeProvider.GetUtcNow();

        return await signer.SignAsync(new JwtContent(
            Options.Issuer, Audience: null, TokenType: "at+jwt", claims, now, now + client.AccessTokenLifetime));
    }

    // A null tenantId (the user has no organization in the client's target yet) gives a token without
    // tenant_id and roles.
    public async Task<string> CreateIdTokenAsync(Client client, Guid userId, Guid? tenantId, IReadOnlyCollection<string> scopeNames, string? nonce)
    {
        var lifetime = client.IdTokenLifetime
            ?? throw new InvalidOperationException($"Client {client.ClientId} does not get ID tokens.");

        var user = await users.GetByIdAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} was not found when creating an ID token.");

        var claims = new Dictionary<string, object>
        {
            ["sub"] = user.Id.ToString()
        };

        if (tenantId is { } tenant)
        {
            claims["tenant_id"] = tenant.ToString();

            var roles = await roleRepository.GetTenantRoleNamesAsync(user.Id, tenant);
            if (roles.Count > 0)
            {
                claims["role"] = roles.ToArray();
            }
        }

        if (!string.IsNullOrEmpty(nonce))
        {
            claims["nonce"] = nonce;
        }

        if (scopeNames.Contains("profile"))
        {
            // There is no separate name: the email is the login identifier and the display name.
            claims["name"] = user.Email;
            claims["preferred_username"] = user.Email;
        }

        if (scopeNames.Contains("email"))
        {
            claims["email"] = user.Email;
        }

        var now = timeProvider.GetUtcNow();

        return await signer.SignAsync(new JwtContent(
            Options.Issuer, client.ClientId, TokenType: null, claims, now, now + lifetime));
    }

    // A null familyId starts a new family (a new sign-in). Returns the raw token.
    public async Task<string> CreateRefreshTokenAsync(Client client, Guid userId, Guid? tenantId, IEnumerable<string> scopeNames, Guid? familyId)
    {
        var lifetime = client.RefreshTokenLifetime
            ?? throw new InvalidOperationException($"Client {client.ClientId} does not get refresh tokens.");

        var (token, rawToken) = RefreshToken.Create(
            clientId: client.Id,
            userId: userId,
            tenantId: tenantId,
            scopes: scopeNames,
            familyId: familyId,
            lifetime: lifetime,
            now: timeProvider.GetUtcNow());

        await refreshTokens.CreateAsync(token);
        return rawToken;
    }

    // Exchanges the refresh token: marks it exchanged and returns it, so that the caller can
    // issue a new token in the same family. Returns null if the token is not valid. Reuse of an
    // already exchanged token revokes the whole family.
    // Documented exception: returns a domain object (see CLAUDE.md).
    public async Task<RefreshToken?> RenewRefreshTokenAsync(string rawToken, Guid clientId, IReadOnlyCollection<string> requestedScopes)
    {
        var now = timeProvider.GetUtcNow();
        var token = await refreshTokens.GetByHashAsync(OpaqueToken.Hash(rawToken));

        if (token is null || token.ClientId != clientId)
            return null;

        // A new refresh token has already been granted: the token has leaked.
        if (token.IsExchanged)
        {
            await refreshTokens.RevokeFamilyAsync(token.FamilyId, now);
            return null;
        }

        if (!token.CanBeRenewed(requestedScopes, now))
            return null;

        // Another request exchanged the token after it was read.
        if (!await refreshTokens.TryMarkExchangedAsync(token.Id, now))
        {
            await refreshTokens.RevokeFamilyAsync(token.FamilyId, now);
            return null;
        }

        return token;
    }

    public Task RevokeFamilyAsync(Guid familyId) =>
        refreshTokens.RevokeFamilyAsync(familyId, timeProvider.GetUtcNow());

    // JSON Web Key Set of the signing keys (/.well-known/jwks.json).
    public async Task<object> GetJwksAsync() =>
        new { keys = await signingKeys.GetPublicJwksAsync() };
}
