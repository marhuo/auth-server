namespace AuthServer.Domain.OAuth;

public interface IAuthorizationCodeRepository
{
    Task<AuthorizationCode?> GetByHashAsync(string codeHash);

    Task CreateAsync(AuthorizationCode code);

    // Atomically marks an unused code as used. Returns false if another request used it first.
    Task<bool> TryMarkUsedAsync(Guid id, DateTimeOffset now);

    // Physically deletes codes that expired before the given time. Returns the number of deleted codes.
    Task<int> DeleteExpiredAsync(DateTimeOffset expiredBefore);
}
