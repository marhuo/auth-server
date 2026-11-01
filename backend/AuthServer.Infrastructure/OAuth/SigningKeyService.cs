using System.Buffers.Text;
using System.Security.Cryptography;
using AuthServer.Domain.OAuth;
using AuthServer.Infrastructure.Persistence;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Infrastructure.OAuth;

// RSA signing keys stored in the database. The private key is encrypted with ASP.NET Core
// Data Protection.
public class SigningKeyService(AuthDbContext db, IDataProtectionProvider dataProtection, TimeProvider timeProvider)
    : ITokenSigner, ISigningKeyManager
{
    private const string Algorithm = "RS256";
    private static readonly TimeSpan RetiredKeyRetention = TimeSpan.FromDays(1);

    private IDataProtector Protector => dataProtection.CreateProtector("AuthServer.SigningKeys.v1");

    public async Task EnsureActiveKeyAsync()
    {
        if (await db.SigningKeys.AnyAsync(k => k.IsActive))
            return;

        try
        {
            await CreateActiveKeyAsync();
        }
        catch (DbUpdateException)
        {
            // Another instance created the key at the same time (unique index on is_active).
            db.ChangeTracker.Clear();

            if (!await db.SigningKeys.AnyAsync(k => k.IsActive))
                throw;
        }
    }

    public async Task RotateAsync()
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        var now = timeProvider.GetUtcNow();
        var active = await db.SigningKeys.Where(k => k.IsActive).ToListAsync();

        foreach (var key in active)
        {
            key.IsActive = false;
            key.RetiredAt = now;
        }

        await db.SaveChangesAsync();
        await CreateActiveKeyAsync();
        await transaction.CommitAsync();
    }

    // The signing_keys table is tiny (the active key and a few retired ones), so this query
    // needs no index beyond is_active.
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> GetPublicJwksAsync()
    {
        var threshold = timeProvider.GetUtcNow() - RetiredKeyRetention;

        var keys = await db.SigningKeys
            .AsNoTracking()
            .Where(k => k.IsActive || k.RetiredAt > threshold)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();

        return keys.Select(ToJwk).ToList();
    }

    public async Task<string> SignAsync(JwtContent content)
    {
        var key = await db.SigningKeys
            .AsNoTracking()
            .Where(k => k.IsActive)
            .OrderByDescending(k => k.CreatedAt)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No active signing key.");

        using var rsa = RSA.Create();
        rsa.ImportFromPem(Protector.Unprotect(key.PrivateKeyPem));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = content.Issuer,
            Audience = content.Audience,
            Claims = content.Claims.ToDictionary(),
            IssuedAt = content.IssuedAt.UtcDateTime,
            NotBefore = content.IssuedAt.UtcDateTime,
            Expires = content.ExpiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(rsa) { KeyId = key.KeyId },
                SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            }
        };

        if (content.TokenType is not null)
            descriptor.TokenType = content.TokenType;

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private async Task CreateActiveKeyAsync()
    {
        using var rsa = RSA.Create(2048);

        var keyId = Base64Url.EncodeToString(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo()));

        db.SigningKeys.Add(new SigningKeyEntity
        {
            Id = Guid.CreateVersion7(),
            KeyId = keyId,
            Algorithm = Algorithm,
            PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem(),
            PrivateKeyPem = Protector.Protect(rsa.ExportPkcs8PrivateKeyPem()),
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow()
        });

        await db.SaveChangesAsync();
    }

    private static IReadOnlyDictionary<string, string> ToJwk(SigningKeyEntity key)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(key.PublicKeyPem);
        var parameters = rsa.ExportParameters(false);

        return new Dictionary<string, string>
        {
            ["kty"] = "RSA",
            ["use"] = "sig",
            ["alg"] = key.Algorithm,
            ["kid"] = key.KeyId,
            ["n"] = Base64Url.EncodeToString(parameters.Modulus!),
            ["e"] = Base64Url.EncodeToString(parameters.Exponent!)
        };
    }
}
