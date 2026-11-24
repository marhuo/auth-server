using AuthServer.Domain.OAuth;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence;

public class RefreshTokenRepository(AuthDbContext db) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByHashAsync(string tokenHash)
    {
        var entity = await db.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == tokenHash);
        return entity?.ToDomain();
    }

    public async Task CreateAsync(RefreshToken token)
    {
        db.RefreshTokens.Add(RefreshTokenEntity.FromDomain(token));
        await db.SaveChangesAsync();
    }

    public async Task<bool> TryMarkExchangedAsync(Guid id, DateTimeOffset now) =>
        await db.RefreshTokens
            .Where(t => t.Id == id && t.ExchangedAt == null && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExchangedAt, now)) == 1;

    public Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now) =>
        db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));

    public Task RevokeRefreshTokensAsync(Guid userId, DateTimeOffset now) =>
        db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));

    public Task<int> DeleteExpiredAsync(DateTimeOffset expiredBefore) =>
        db.RefreshTokens
            .Where(t => t.ExpiresAt < expiredBefore)
            .ExecuteDeleteAsync();
}
