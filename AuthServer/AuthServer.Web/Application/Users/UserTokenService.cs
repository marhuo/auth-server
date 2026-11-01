using Microsoft.EntityFrameworkCore;
using AuthServer.Web.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;
namespace AuthServer.Web.Application.Users;

public class UserTokenService(AuthDbContext db, TimeProvider timeProvider)
{
    private static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);

    public async Task<string> CreateInviteTokenAsync(Guid userId)
    {
        var now = timeProvider.GetUtcNow();

        // Invalidate earlier unused invite tokens
        await db.UserTokens
            .Where(t => t.UserId == userId && t.Purpose == "invite" && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now));

        var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

        db.UserTokens.Add(new UserTokenEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Purpose = "invite",
            TokenHash = HashToken(rawToken),
            ExpiresAt = now.Add(InviteLifetime)
        });
        await db.SaveChangesAsync();

        return rawToken;
    }

    public async Task<Guid?> FindUserIdByInviteTokenAsync(string rawToken)
    {
        var hash = HashToken(rawToken);
        var now = timeProvider.GetUtcNow();

        return await db.UserTokens
            .Where(t => t.TokenHash == hash && t.Purpose == "invite" && t.UsedAt == null && t.ExpiresAt > now)
            .Select(t => (Guid?)t.UserId)
            .SingleOrDefaultAsync();
    }

    public async Task MarkUsedAsync(string rawToken)
    {
        var hash = HashToken(rawToken);
        var now = timeProvider.GetUtcNow();

        await db.UserTokens
            .Where(t => t.TokenHash == hash)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now));
    }

    public static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    public async Task DeleteExpiredAsync()
    {
        var now = timeProvider.GetUtcNow();
        await db.UserTokens
            .Where(t => t.UsedAt != null || t.ExpiresAt < now)
            .ExecuteDeleteAsync();
    }
}