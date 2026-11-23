using AuthServer.Application.Users.Services;
using AuthServer.Domain.Common;
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

    // Sign-in with an external service, e.g. Google. The first sign-in creates the user, or links the
    // user with the same email if the service has verified the email.
    public async Task<(ExternalSignInStatus Status, User? User)> SignInExternalAsync(ExternalIdentity identity)
    {
        var now = timeProvider.GetUtcNow();
        var user = await users.GetByExternalIdAsync(identity.Provider, identity.ProviderUserId);

        if (user is null)
        {
            // An unverified email may belong to someone else.
            if (!identity.EmailVerified)
                return (ExternalSignInStatus.EmailNotVerified, null);

            user = await users.GetByNormalizedEmailAsync(User.NormalizeEmail(identity.Email));
            if (user is null)
            {
                user = User.CreateExternal(identity: identity, now: now);
                await users.CreateAsync(user);
            }
            else
            {
                user.AddExternalId(identity, now);
                await users.UpdateAsync(user);
            }
        }

        return user.CanSignIn(now) ? (ExternalSignInStatus.Success, user) : (ExternalSignInStatus.Locked, null);
    }

    public Task<User?> GetByIdAsync(Guid id) =>
        users.GetByIdAsync(id);

    public Task<List<string>> GetAuthServerRoleNamesAsync(Guid userId) =>
        roles.GetAuthServerRoleNamesAsync(userId);

    // Sends a password reset link.
    // Returns nothing on purpose: otherwise anyone could check which emails are registered.
    public async Task RequestPasswordResetAsync(string email)
    {
        var user = string.IsNullOrWhiteSpace(email)
            ? null
            : await users.GetByNormalizedEmailAsync(User.NormalizeEmail(email));

        // A locked user could not sign in with a new password either.
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

    // Registers a user and sends a link to set the password.
    // If email is taken does nothing: otherwise anyone could check which emails are registered.
    public async Task RegisterAsync(string email, string? returnUrl)
    {
        email = email.Trim();

        var errors = new ErrorCollector();
        User.ValidateEmail(errors, email);
        errors.ThrowIfAny();

        var now = timeProvider.GetUtcNow();
        var normalizedEmail = User.NormalizeEmail(email);
        var user = await users.GetByNormalizedEmailAsync(normalizedEmail);

        if (user is null)
        {
            user = User.CreateInvited(
                email: email,
                now: now);
            await users.CreateAsync(user);
        }
        else if (user.Locked)
        {
            return;
        }
        // Already registered: a reminder to sign in instead.
        else if (user.Status == UserStatuses.Active)
        {
            await emailSender.SendAlreadyRegisteredAsync(user.Email, returnUrl);
            return;
        }

        // A new user, or one who registered earlier but did not set a password.
        var (token, rawToken) = PasswordToken.Create(
            userId: user.Id,
            email: user.Email,
            type: PasswordTokenTypes.Invitation,
            now: now);

        await passwordTokens.CreateAsync(token);
        await emailSender.SendRegistrationAsync(user.Email, rawToken, returnUrl);
    }

    // Checks a password link. Returns its type, or null if the link is unknown, used or expired.
    public async Task<PasswordTokenTypes?> GetUsablePasswordTokenTypeAsync(string rawToken)
    {
        var token = await passwordTokens.GetByTokenHashAsync(PasswordToken.HashToken(rawToken));
        return token is not null && token.IsUsable(timeProvider.GetUtcNow()) ? token.Type : null;
    }

    // Sets the password with a link token. Returns the user, or null if the link is not valid.
    // A rejected password keeps the link usable. Also revokes the user's refresh tokens.
    public async Task<User?> SetPasswordWithTokenAsync(string rawToken, string password)
    {
        userValidator.ValidatePassword(password);

        var now = timeProvider.GetUtcNow();
        var token = await passwordTokens.GetByTokenHashAsync(PasswordToken.HashToken(rawToken));

        if (token is null || !token.IsUsable(now))
            return null;

        var user = await users.GetByIdAsync(token.UserId);
        if (user is null)
            return null;

        user.SetPassword(password, hasher);
        token.MarkUsed(now);

        await users.UpdateAsync(user);
        await passwordTokens.UpdateAsync(token);
        await refreshTokens.RevokeRefreshTokensAsync(user.Id, now);

        return user;
    }
}
