using AuthServer.Application.Common.Messages;
using AuthServer.Application.Users.Messages;
using AuthServer.Domain.Common;
using AuthServer.Domain.OAuth;
using AuthServer.Domain.ResourceServers;
using AuthServer.Domain.Roles;
using AuthServer.Domain.Users;

namespace AuthServer.Application.Users.Services;

public class UserService(
    IUserRepository users,
    IRoleRepository roles,
    IResourceServerRepository resourceServers,
    IPasswordTokenRepository passwordTokens,
    IRefreshTokenRepository refreshTokens,
    UserValidator validator,
    IPasswordHasher hasher,
    IUserEmailSender emailSender,
    TimeProvider timeProvider)
{
    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await users.GetByIdAsync(id);
        return user is null ? null : UserDto.ToDto(user);
    }

    public async Task<List<UserDto>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        var list = await users.GetByIdsAsync(ids);
        return list.Select(UserDto.ToDto).ToList();
    }

    public async Task<PagedListDto<UserDto>> GetPagedListAsync(UserPagedListRequest request)
    {
        var pagedList = await users.GetPagedListAsync(new UserPagedListQuery
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SearchTerm = request.SearchTerm,
            TenantId = request.TenantId
        });

        return pagedList.ToDto(UserDto.ToDto);
    }

    // Users are created only by invitation. Creates the user if the email is unknown, adds the roles,
    // and sends an invitation link when the user has no password yet. Returns the id of the user,
    // existing or new.
    public async Task<Guid> InviteAsync(UserInviteRequest request)
    {
        await validator.ValidateInviteAsync(request);

        var now = timeProvider.GetUtcNow();
        var user = await users.GetByNormalizedEmailAsync(User.NormalizeEmail(request.Email));
        var isNew = user is null;

        if (user is null)
        {
            await validator.ValidateEmailNotTakenAsync(request.Email);
            user = User.CreateInvited(
                email: request.Email,
                now: now);
        }

        foreach (var role in request.UserRoles)
            user.AddRole(role.RoleId, role.TenantId, now);

        if (isNew)
            await users.CreateAsync(user);
        else
            await users.UpdateAsync(user);

        if (user.Status == UserStatuses.Invited)
            await SendInvitationAsync(user, request.ClientId, now);

        return user.Id;
    }

    // Sends a new invitation link to a user who has not set a password yet, e.g. when the first email
    // went to spam. The earlier links stop working.
    public async Task ResendInviteAsync(Guid id)
    {
        var user = await LoadAsync(id, nameof(id));

        validator.ValidateResendInvite(user);

        await SendInvitationAsync(user, clientId: null, timeProvider.GetUtcNow());
    }

    // Sends a link to set the password. A new link invalidates the user's earlier unused links.
    private async Task SendInvitationAsync(User user, string? clientId, DateTimeOffset now)
    {
        var (token, rawToken) = PasswordToken.Create(
            userId: user.Id,
            email: user.Email,
            type: PasswordTokenTypes.Invitation,
            now: now);
        await passwordTokens.CreateAsync(token);
        await emailSender.SendInviteAsync(user.Email, rawToken, clientId);
    }

    public async Task UpdateAsync(UserUpdateRequest request)
    {
        var user = await LoadAsync(request.Id, nameof(request.Id));

        await validator.ValidateUpdateAsync(user, request);

        user.SetEmail(request.Email);
        user.SetLocked(request.Locked);

        await users.UpdateAsync(user);
    }

    // Also revokes the user's refresh tokens, so other sessions must sign in again.
    public async Task ChangePasswordAsync(UserChangePasswordRequest request)
    {
        validator.ValidatePassword(request.NewPassword);

        var user = await LoadAsync(request.Id, nameof(request.Id));

        user.SetPassword(request.NewPassword, hasher);

        await users.UpdateAsync(user);
        await refreshTokens.RevokeRefreshTokensAsync(user.Id, timeProvider.GetUtcNow());
    }

    public async Task AddRoleAsync(AddUserRoleRequest request)
    {
        var user = await LoadAsync(request.UserId, nameof(request.UserId));

        await validator.ValidateAddRoleAsync(user, request);

        user.AddRole(request.RoleId, request.TenantId, timeProvider.GetUtcNow());

        await users.UpdateAsync(user);
    }

    // Users are never deleted; access is removed by removing roles. Also revokes the user's refresh
    // tokens, so new tokens contain only the remaining roles.
    public async Task RemoveRoleAsync(RemoveUserRoleRequest request)
    {
        var user = await users.GetByIdAsync(request.UserId);

        if (user is null || !user.HasRole(request.RoleId, request.TenantId))
            return;

        await validator.ValidateRemoveRoleAsync(user, request);

        user.RemoveRole(request.RoleId, request.TenantId);

        await users.UpdateAsync(user);
        await refreshTokens.RevokeRefreshTokensAsync(user.Id, timeProvider.GetUtcNow());
    }

    // Removes the user's roles of one product in one tenant, e.g. when the user is removed from a
    // workspace. Other products' roles stay. Also revokes the user's refresh tokens.
    public async Task RemoveFromTenantAsync(UserRemoveFromTenantRequest request)
    {
        var user = await LoadAsync(request.UserId, nameof(request.UserId));
        var resourceServer = await resourceServers.GetByAudienceAsync(request.ResourceServerAudience)
            ?? throw new ValidationException(Errors.NotFound(
                nameof(request.ResourceServerAudience), "Resource server", "Resurssipalvelinta"));

        validator.ValidateRemoveFromTenant(resourceServer);

        var roleIds = await roles.GetIdsByResourceServerAsync(resourceServer.Id);
        if (!user.RemoveRolesInTenant(roleIds, request.TenantId))
            return;

        await users.UpdateAsync(user);
        await refreshTokens.RevokeRefreshTokensAsync(user.Id, timeProvider.GetUtcNow());
    }

    // Deletes users who never started using their account, e.g. an unfinished registration.
    // Returns the number of deleted users.
    public Task<int> DeleteUnusedInvitedAsync()
    {
        var now = timeProvider.GetUtcNow();
        return users.DeleteUnusedInvitedAsync(now - User.UnusedInvitedRetention, now);
    }

    private async Task<User> LoadAsync(Guid id, string key) =>
        await users.GetByIdAsync(id)
        ?? throw new ValidationException(Errors.NotFound(key, "User", "Käyttäjää"));
}
