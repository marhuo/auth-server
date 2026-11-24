using AuthServer.Domain.OAuth;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence;

public class AuthorizationCodeRepository(AuthDbContext db) : IAuthorizationCodeRepository
{
    public async Task<AuthorizationCode?> GetByHashAsync(string codeHash)
    {
        var entity = await db.AuthorizationCodes.AsNoTracking().SingleOrDefaultAsync(c => c.CodeHash == codeHash);
        return entity?.ToDomain();
    }

    public async Task CreateAsync(AuthorizationCode code)
    {
        db.AuthorizationCodes.Add(AuthorizationCodeEntity.FromDomain(code));
        await db.SaveChangesAsync();
    }

    public async Task<bool> TryMarkUsedAsync(Guid id, DateTimeOffset now) =>
        await db.AuthorizationCodes
            .Where(c => c.Id == id && c.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedAt, now)) == 1;

    public Task<int> DeleteExpiredAsync(DateTimeOffset expiredBefore) =>
        db.AuthorizationCodes
            .Where(c => c.ExpiresAt < expiredBefore)
            .ExecuteDeleteAsync();
}
