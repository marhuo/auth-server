using AuthServer.Application.Users.Services;
using AuthServer.Domain.OAuth;
using AuthServer.Domain.Roles;
using AuthServer.Domain.Users;

namespace AuthServer.Application.OAuth.Services;

// User sign-in for the login, password reset and set password pages, the OAuth endpoints and the
// sign-in cookie validation. Returns the domain User because they need its behavior (CanSignIn,
// lockout). Documented exception: the OAuth namespace may return domain objects (see CLAUDE.md).
public class UserAuthenticationService(
    IUserRepository users,
    IRoleRepository roles,
    IPasswordTokenRepository passwordTokens,
    IRefreshTokenRepository refreshTokens,
    UserValidator userValidator,
    IPasswordHasher hasher,
    IUserEmailSender emailSender,
    TimeProvider timeProvider)
{
    // Checks the password and applies the lockout policy (see User.Login).
    public async Task<(LoginStatus Status, User? User)> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            return (LoginStatus.Failed, null);

        var user = await users.GetByNormalizedEmailAsync(User.NormalizeEmail(email));
        if (user is null)
            return (LoginStatus.Failed, null);

        var status = user.Login(password, hasher, timeProvider.GetUtcNow());

        // Failed attempts, lockout and rehashed passwords are stored.
        await users.UpdateAsync(user);

        return status == LoginStatus.Success ? (status, user) : (status, null);
    }

    public Task<User?> GetByIdAsync(Guid id) =>
        users.GetByIdAsync(id);

    public Task<List<string>> GetAuthServerRoleNamesAsync(Guid userId) =>
        roles.GetAuthServerRoleNamesAsync(userId);

    // "Forgot password": sends a reset link if the email belongs to a user. Returns nothing and never
    // fails for an unknown email, so the page cannot be used to find out which emails have an account.
    // An admin-locked user gets no link: a new password would not let them sign in.
    public async Task RequestPasswordResetAsync(string email)
    {
        var user = string.IsNullOrWhiteSpace(email)
            ? null
            : await users.GetByNormalizedEmailAsync(User.NormalizeEmail(email));

        if (user is null || user.Locked)
            return;

        var (token, rawToken) = PasswordToken.Create(
            userId: user.Id,
            email: user.Email,
            type: PasswordTokenTypes.Reset,
            now: timeProvider.GetUtcNow());

        await passwordTokens.CreateAsync(token);
        await emailSender.SendPasswordResetAsync(user.Email, rawToken);
    }

    // The type of a usable password token (invitation or reset), or null if the link is not valid.
    public async Task<PasswordTokenTypes?> GetUsablePasswordTokenTypeAsync(string rawToken)
    {
        var token = await passwordTokens.GetByTokenHashAsync(PasswordToken.HashToken(rawToken));
        return token is not null && token.IsUsable(timeProvider.GetUtcNow()) ? token.Type : null;
    }

    // Sets the password with an invitation or reset link. Returns false if the link is not valid.
    // Throws ValidationException if the password does not meet the requirements; the link then stays
    // usable. Also ends the automatic lockout (User.SetPassword) and revokes the user's refresh tokens.
    public async Task<bool> SetPasswordWithTokenAsync(string rawToken, string password)
    {
        userValidator.ValidatePassword(password);

        var now = timeProvider.GetUtcNow();
        var token = await passwordTokens.GetByTokenHashAsync(PasswordToken.HashToken(rawToken));

        if (token is null || !token.IsUsable(now))
            return false;

        var user = await users.GetByIdAsync(token.UserId);
        if (user is null)
            return false;

        user.SetPassword(password, hasher);
        token.MarkUsed(now);

        await users.UpdateAsync(user);
        await passwordTokens.UpdateAsync(token);
        await refreshTokens.RevokeRefreshTokensAsync(user.Id, now);

        return true;
    }
}
