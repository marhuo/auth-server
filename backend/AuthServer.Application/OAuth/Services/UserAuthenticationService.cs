using AuthServer.Domain.Common;
using AuthServer.Domain.OAuth;
using AuthServer.Domain.Roles;
using AuthServer.Domain.Users;

namespace AuthServer.Application.OAuth.Services;

// User sign-in with an emailed code or an external service, for the sign-in pages, the OAuth endpoints
// and the sign-in cookie validation. Returns the domain User because they need its behavior
// (CanSignIn). Documented exception: the OAuth namespace may return domain objects (see CLAUDE.md).
public class UserAuthenticationService(
    IUserRepository users,
    IRoleRepository roles,
    ISignInCodeRepository signInCodes,
    IUserEmailSender emailSender,
    TimeProvider timeProvider)
{
    // Sends a sign-in code to the email, whether or not it has a user yet. returnUrl goes into the
    // email link (see IUserEmailSender.SendSignInCodeAsync).
    // Returns nothing on purpose: otherwise anyone could check which emails are registered.
    public async Task SendSignInCodeAsync(string email, string? returnUrl)
    {
        email = email.Trim();

        var errors = new ErrorCollector();
        User.ValidateEmail(errors, email);
        errors.ThrowIfAny();

        // A locked user could not sign in with the code either.
        var user = await users.GetByNormalizedEmailAsync(User.NormalizeEmail(email));
        if (user is { Locked: true })
            return;

        var (code, rawCode) = SignInCode.Create(email, timeProvider.GetUtcNow());

        await signInCodes.CreateAsync(code);
        await emailSender.SendSignInCodeAsync(email, rawCode, returnUrl);
    }

    // Checks the code sent to the email. Creates the user on the first sign-in. Returns null if the
    // code is wrong, used or expired, or the user cannot sign in.
    public async Task<User?> SignInWithCodeAsync(string email, string rawCode)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(rawCode))
            return null;

        var now = timeProvider.GetUtcNow();
        var normalizedEmail = User.NormalizeEmail(email);

        var code = await signInCodes.GetLatestAsync(normalizedEmail);
        if (code is null || !code.IsUsable(now))
            return null;

        var correct = code.Verify(rawCode, now);
        await signInCodes.UpdateAsync(code);

        if (!correct)
            return null;

        var user = await users.GetByNormalizedEmailAsync(normalizedEmail);
        if (user is null)
        {
            user = User.CreateInvited(email: email.Trim(), now: now);
            user.Activate();
            await users.CreateAsync(user);
        }
        else if (user.Status != UserStatuses.Active)
        {
            user.Activate();
            await users.UpdateAsync(user);
        }

        return user.CanSignIn ? user : null;
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

        return user.CanSignIn ? (ExternalSignInStatus.Success, user) : (ExternalSignInStatus.Locked, null);
    }

    public Task<User?> GetByIdAsync(Guid id) =>
        users.GetByIdAsync(id);

    public Task<List<string>> GetAuthServerRoleNamesAsync(Guid userId) =>
        roles.GetAuthServerRoleNamesAsync(userId);
}
