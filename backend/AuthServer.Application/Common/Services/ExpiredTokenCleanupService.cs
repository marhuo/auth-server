using AuthServer.Domain.OAuth;
using AuthServer.Domain.Users;

namespace AuthServer.Application.Common.Services;

// Physically deletes authorization codes, refresh tokens and password tokens that expired more than
// ExpiredTokenRetention ago. Run nightly by ExpiredTokenCleanupJob.
public class ExpiredTokenCleanupService(
    IAuthorizationCodeRepository authorizationCodes,
    IRefreshTokenRepository refreshTokens,
    IPasswordTokenRepository passwordTokens,
    TimeProvider timeProvider)
{
    // Expired tokens are kept for a while so that recent token history can still be investigated.
    private static readonly TimeSpan ExpiredTokenRetention = TimeSpan.FromDays(7);

    // Returns the total number of deleted rows.
    public async Task<int> DeleteExpiredTokensAsync()
    {
        var expiredBefore = timeProvider.GetUtcNow() - ExpiredTokenRetention;

        return await authorizationCodes.DeleteExpiredAsync(expiredBefore)
            + await refreshTokens.DeleteExpiredAsync(expiredBefore)
            + await passwordTokens.DeleteExpiredAsync(expiredBefore);
    }
}
