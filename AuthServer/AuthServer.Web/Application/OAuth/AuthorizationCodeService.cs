// AuthorizationCodeService.cs
using AuthServer.Web.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace AuthServer.Web.Application.OAuth;

public class AuthorizationCodeService(AuthDbContext db)
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);

    public async Task<string> CreateAsync(
        Guid clientId,
        Guid userId,
        Guid tenantId,
        string redirectUri,
        IEnumerable<string> scopes,
        string codeChallenge,
        string? nonce)
    {
        var code = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var now = DateTimeOffset.UtcNow;

        db.AuthorizationCodes.Add(new AuthorizationCodeEntity
        {
            Id = Guid.CreateVersion7(),
            CodeHash = Hash(code),
            ClientId = clientId,
            UserId = userId,
            TenantId = tenantId,
            RedirectUri = redirectUri,
            Scopes = scopes.ToList(),
            CodeChallenge = codeChallenge,
            Nonce = nonce,
            CreatedAt = now,
            ExpiresAt = now + Lifetime
        });

        await db.SaveChangesAsync();
        return code;
    }

    public async Task<AuthorizationCodeInfo?> UseCodeAsync(string code, Guid clientId, string redirectUri, string verifier)
    {
        var hash = Hash(code);
        var authorizationCodeEntity = await db.AuthorizationCodes.AsNoTracking()
            .SingleOrDefaultAsync(c => c.CodeHash == hash);

        var now = DateTimeOffset.UtcNow;

        if (authorizationCodeEntity is null
            || authorizationCodeEntity.ClientId != clientId
            || authorizationCodeEntity.UsedAt is not null
            || authorizationCodeEntity.ExpiresAt <= now
            || !string.Equals(authorizationCodeEntity.RedirectUri, redirectUri, StringComparison.Ordinal)
            || !PkceMatches(verifier, authorizationCodeEntity.CodeChallenge))
            return null;

        var consumed = await db.AuthorizationCodes
            .Where(c => c.Id == authorizationCodeEntity.Id && c.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedAt, now));

        if (consumed != 1)
            return null;

        return new AuthorizationCodeInfo
        {
            UserId = authorizationCodeEntity.UserId,
            TenantId = authorizationCodeEntity.TenantId,
            Scopes = authorizationCodeEntity.Scopes.ToList(),
            Nonce = authorizationCodeEntity.Nonce
        };
    }

    private static bool PkceMatches(string verifier, string challenge)
    {
        var computed = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computed),
            Encoding.ASCII.GetBytes(challenge));
    }

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}