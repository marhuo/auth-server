namespace AuthServer.Domain.Users;

public interface IPasswordTokenRepository
{
    Task<PasswordToken?> GetByTokenHashAsync(string tokenHash);

    // Invalidates the user's earlier unused tokens of any type (sets UsedAt) and stores the new one,
    // so an invitation and a reset link are never usable at the same time.
    Task CreateAsync(PasswordToken token);

    Task UpdateAsync(PasswordToken token);

    // Physically deletes tokens that expired before the given time. Returns the number of deleted tokens.
    Task<int> DeleteExpiredAsync(DateTimeOffset expiredBefore);
}
