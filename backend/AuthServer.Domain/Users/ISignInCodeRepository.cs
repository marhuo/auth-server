namespace AuthServer.Domain.Users;

public interface ISignInCodeRepository
{
    // The newest code sent to the email, or null.
    Task<SignInCode?> GetLatestAsync(string normalizedEmail);

    // Invalidates the email's earlier unused codes (sets UsedAt) and stores the new one.
    Task CreateAsync(SignInCode code);

    Task UpdateAsync(SignInCode code);

    // Physically deletes codes that expired before the given time. Returns the number of deleted codes.
    Task<int> DeleteExpiredAsync(DateTimeOffset expiredBefore);
}
