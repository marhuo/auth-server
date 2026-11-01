using AuthServer.Domain.Users;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence;

public class PasswordTokenRepository(AuthDbContext db) : IPasswordTokenRepository
{
    public async Task<PasswordToken?> GetByTokenHashAsync(string tokenHash)
    {
        var entity = await db.PasswordTokens.AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == tokenHash);
        return entity?.ToDomain();
    }

    public async Task CreateAsync(PasswordToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.PasswordTokens
            .Where(t => t.UserId == token.UserId && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, token.CreatedAt));

        db.PasswordTokens.Add(PasswordTokenEntity.FromDomain(token));
        await db.SaveChangesAsync();

        await transaction.CommitAsync();
    }

    public async Task UpdateAsync(PasswordToken token)
    {
        var entity = await db.PasswordTokens.SingleAsync(t => t.Id == token.Id);
        entity.UpdateFrom(token);
        await db.SaveChangesAsync();
    }

    public Task<int> DeleteExpiredAsync(DateTimeOffset expiredBefore) =>
        db.PasswordTokens
            .Where(t => t.ExpiresAt < expiredBefore)
            .ExecuteDeleteAsync();
}
