namespace AuthServer.Domain.OAuth;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash);

    Task CreateAsync(RefreshToken token);

    // Atomically marks a token that is neither exchanged nor revoked as exchanged.
    // Returns false if another request exchanged it first.
    Task<bool> TryMarkExchangedAsync(Guid id, DateTimeOffset now);

    Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now);

    // Revokes the user's active refresh tokens, e.g. after a role is removed.
    Task RevokeRefreshTokensAsync(Guid userId, DateTimeOffset now);

    // Physically deletes tokens that expired before the given time. Returns the number of deleted tokens.
    Task<int> DeleteExpiredAsync(DateTimeOffset expiredBefore);
}
