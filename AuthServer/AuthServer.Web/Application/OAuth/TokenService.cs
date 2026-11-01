using System.Security.Cryptography;
using AuthServer.Web.Application.Clients;
using AuthServer.Web.Application.Scopes;
using AuthServer.Web.Application.Users;
using AuthServer.Web.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Web.Application.OAuth;

public class TokenService(
    AuthDbContext db,
    UserService userService,
    SigningKeyService signingKeys,
    ScopeService scopeService,
    IOptions<AuthTokenOptions> options)
{
    private AuthTokenOptions Options => options.Value;

    public async Task<string> CreateAccessTokenAsync(Client client, Guid? userId, Guid? tenantId, IReadOnlyCollection<string> scopes)
    {
        var scopeList = scopes.ToList();

        var audiences = await scopeService.GetAudiencesAsync(scopes);

        var claims = new Dictionary<string, object>
        {
            ["sub"] = userId?.ToString() ?? client.ClientId,
            ["client_id"] = client.ClientId,
            ["scope"] = string.Join(' ', scopeList),
            ["jti"] = Guid.NewGuid().ToString("N")
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
                var roles = await userService.GetTenantRolesAsync(user, tenant);
                if (roles.Count > 0)
                {
                    claims["role"] = roles.ToArray();
                }
            }
        }

        var now = DateTime.UtcNow;

        return await signingKeys.SignAsync(new SecurityTokenDescriptor
        {
            Issuer = Options.Issuer,
            Claims = claims,
            TokenType = "at+jwt",
            IssuedAt = now,
            NotBefore = now,
            Expires = now + Options.AccessTokenLifetime
        });
    }

    public async Task<string> CreateIdTokenAsync(Client client, User user, Guid tenantId, IReadOnlyCollection<string> scopes, string? nonce)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = user.Id.ToString(),
            ["tenant_id"] = tenantId.ToString()
        };

        var roles = await userService.GetTenantRolesAsync(user.Id, tenantId);
        if (roles.Count > 0)
        {
            claims["role"] = roles.ToArray();
        }

        if (!string.IsNullOrEmpty(nonce))
        {
            claims["nonce"] = nonce;
        }

        if (scopes.Contains("profile"))
        {
            claims["name"] = user.UserName;
        }

        if (scopes.Contains("email"))
        {
            claims["email"] = user.Email;
        }

        var now = DateTime.UtcNow;

        return await signingKeys.SignAsync(new SecurityTokenDescriptor
        {
            Issuer = Options.Issuer,
            Audience = client.ClientId,
            Claims = claims,
            IssuedAt = now,
            NotBefore = now,
            Expires = now + Options.IdTokenLifetime
        });
    }

    public async Task<string> CreateRefreshTokenAsync(Guid clientId, Guid userId, Guid tenantId, IEnumerable<string> scopes, Guid? familyId)
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var now = DateTimeOffset.UtcNow;

        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.CreateVersion7(),
            TokenHash = AuthorizationCodeService.Hash(token),
            FamilyId = familyId ?? Guid.CreateVersion7(),
            ClientId = clientId,
            UserId = userId,
            TenantId = tenantId,
            Scopes = scopes.ToList(),
            CreatedAt = now,
            ExpiresAt = now + Options.RefreshTokenLifetime
        });

        await db.SaveChangesAsync();
        return token;
    }

    public async Task<RefreshToken?> RenewRefreshTokenAsync(string serializedRefreshToken, Guid clientId, IReadOnlyCollection<string> requestedScopes)
    {
        var hash = AuthorizationCodeService.Hash(serializedRefreshToken);
        var refreshTokenEntity = await db.RefreshTokens.AsNoTracking()
            .SingleOrDefaultAsync(t => t.TokenHash == hash);

        if (refreshTokenEntity is null || refreshTokenEntity.ClientId != clientId)
            return null;

        // A new refresh token has already been granted
        if (refreshTokenEntity.ExchangedAt is not null)
        {
            await RevokeFamilyAsync(refreshTokenEntity.FamilyId);
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        if (refreshTokenEntity.RevokedAt is not null || refreshTokenEntity.ExpiresAt <= now)
            return null;

        if (requestedScopes.Except(refreshTokenEntity.Scopes).Any())
            return null;

        var consumed = await db.RefreshTokens
            .Where(t => t.Id == refreshTokenEntity.Id && t.ExchangedAt == null && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExchangedAt, now));

        if (consumed != 1)
        {
            await RevokeFamilyAsync(refreshTokenEntity.FamilyId);
            return null;
        }

        return new RefreshToken
        {
            UserId = refreshTokenEntity.UserId,
            TenantId = refreshTokenEntity.TenantId,
            FamilyId = refreshTokenEntity.FamilyId,
            Scopes = refreshTokenEntity.Scopes.ToList()
        };
    }

    public Task RevokeFamilyAsync(Guid familyId)
    {
        var now = DateTimeOffset.UtcNow;

        return db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
    }
}