using System.Security.Cryptography;
using AuthServer.Web.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
// SigningKeyService.cs
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Web.Application.OAuth;

public class SigningKeyService(AuthDbContext db, IDataProtectionProvider dataProtection)
{
    private const string Algorithm = "RS256";
    private static readonly TimeSpan RetiredKeyRetention = TimeSpan.FromDays(1);

    private IDataProtector Protector => dataProtection.CreateProtector("AuthServer.SigningKeys.v1");

    public async Task EnsureActiveKeyAsync()
    {
        if (await db.SigningKeys.AnyAsync(k => k.IsActive))
        {
            return;
        }

        try
        {
            await CreateActiveKeyAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            if (!await db.SigningKeys.AnyAsync(k => k.IsActive))
            {
                throw;
            }
        }
    }

    public async Task RotateAsync()
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        var now = DateTimeOffset.UtcNow;
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

    public async Task<object> GetJwksAsync()
    {
        var threshold = DateTimeOffset.UtcNow - RetiredKeyRetention;

        var keys = await db.SigningKeys
            .AsNoTracking()
            .Where(k => k.IsActive || k.RetiredAt > threshold)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();

        return new { keys = keys.Select(ToJwk).ToList() };
    }

    public async Task<string> SignAsync(SecurityTokenDescriptor descriptor)
    {
        var key = await db.SigningKeys
            .AsNoTracking()
            .Where(k => k.IsActive)
            .OrderByDescending(k => k.CreatedAt)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No active signing key.");

        using var rsa = RSA.Create();
        rsa.ImportFromPem(Protector.Unprotect(key.PrivateKeyPem));

        descriptor.SigningCredentials = new SigningCredentials(
            new RsaSecurityKey(rsa) { KeyId = key.KeyId },
            SecurityAlgorithms.RsaSha256)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private async Task CreateActiveKeyAsync()
    {
        using var rsa = RSA.Create(2048);

        var keyId = WebEncoders.Base64UrlEncode(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo()));

        db.SigningKeys.Add(new SigningKeyEntity
        {
            Id = Guid.CreateVersion7(),
            KeyId = keyId,
            Algorithm = Algorithm,
            PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem(),
            PrivateKeyPem = Protector.Protect(rsa.ExportPkcs8PrivateKeyPem()),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private static Dictionary<string, string> ToJwk(SigningKeyEntity key)
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
            ["n"] = WebEncoders.Base64UrlEncode(parameters.Modulus!),
            ["e"] = WebEncoders.Base64UrlEncode(parameters.Exponent!)
        };
    }
}