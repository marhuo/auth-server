using AuthServer.Domain.Users;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence;

public class SignInCodeRepository(AuthDbContext db) : ISignInCodeRepository
{
    public async Task<SignInCode?> GetLatestAsync(string normalizedEmail)
    {
        var entity = await db.SignInCodes.AsNoTracking()
            .Where(c => c.NormalizedEmail == normalizedEmail)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();
        return entity?.ToDomain();
    }

    public async Task CreateAsync(SignInCode code)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.SignInCodes
            .Where(c => c.NormalizedEmail == code.NormalizedEmail && c.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedAt, code.CreatedAt));

        db.SignInCodes.Add(SignInCodeEntity.FromDomain(code));
        await db.SaveChangesAsync();

        await transaction.CommitAsync();
    }

    public async Task UpdateAsync(SignInCode code)
    {
        var entity = await db.SignInCodes.SingleAsync(c => c.Id == code.Id);
        entity.UpdateFrom(code);
        await db.SaveChangesAsync();
    }

    public Task<int> DeleteExpiredAsync(DateTimeOffset expiredBefore) =>
        db.SignInCodes
            .Where(c => c.ExpiresAt < expiredBefore)
            .ExecuteDeleteAsync();
}
