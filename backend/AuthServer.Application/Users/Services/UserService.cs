using AuthServer.Application.Common.Messages;
using AuthServer.Application.Users.Messages;
using AuthServer.Domain.Clients;
using AuthServer.Domain.Common;
using AuthServer.Domain.OAuth;
using AuthServer.Domain.ResourceServers;
using AuthServer.Domain.Roles;
using AuthServer.Domain.Tenants;
using AuthServer.Domain.Users;

namespace AuthServer.Application.Users.Services;

public class UserService(
    IUserRepository users,
    IRoleRepository roles,
    IResourceServerRepository resourceServers,
    IClientRepository clients,
    ITenantRepository tenants,
    IRefreshTokenRepository refreshTokens,
    UserValidator validator,
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

    // Creates the user if the email is unknown, adds the roles, and sends an invitation email when the
    // user has not signed in yet. Returns the id of the user, existing or new.
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
        {
            var tenantId = request.UserRoles.Select(r => r.TenantId).FirstOrDefault(id => id is not null);
            await SendInvitationAsync(
                user, tenantId, request.ClientId, EmailLanguages.FromCode(request.Language));
        }

        return user.Id;
    }

    // Sends the invitation email again to a user who has not signed in yet, e.g. when the first email
    // went to spam.
    public async Task ResendInviteAsync(Guid id)
    {
        var user = await LoadAsync(id, nameof(id));

        validator.ValidateResendInvite(user);

        var tenantId = user.Roles.Select(r => r.TenantId).FirstOrDefault(id => id is not null);
        await SendInvitationAsync(user, tenantId, clientId: null, EmailLanguage.Fi);
    }

    // The email names the tenant the user joins, if any. It links to the app's sign-in
    // page (InitiateLoginUri) when the client is known, otherwise to the auth server's sign-in page.
    private async Task SendInvitationAsync(
        User user, Guid? tenantId, string? clientId, EmailLanguage language)
    {
        var client = string.IsNullOrWhiteSpace(clientId) ? null : await clients.FindActiveByClientIdAsync(clientId);
        var tenant = tenantId is null ? null : await tenants.GetByIdAsync(tenantId.Value);

        await emailSender.SendInviteAsync(
            user.Email,
            appName: client?.DisplayName,
            tenantName: tenant?.Name,
            appLoginUri: client?.InitiateLoginUri,
            language);
    }

    public async Task UpdateAsync(UserUpdateRequest request)
    {
        var user = await LoadAsync(request.Id, nameof(request.Id));

        await validator.ValidateUpdateAsync(user, request);

        user.SetEmail(request.Email);
        user.SetLocked(request.Locked);

        await users.UpdateAsync(user);
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

    // Deletes invited users who never signed in and no longer have roles.
    // Returns the number of deleted users.
    public Task<int> DeleteUnusedInvitedAsync() =>
        users.DeleteUnusedInvitedAsync(timeProvider.GetUtcNow() - User.UnusedInvitedRetention);

    private async Task<User> LoadAsync(Guid id, string key) =>
        await users.GetByIdAsync(id)
        ?? throw new ValidationException(Errors.NotFound(key, "User", "Käyttäjää"));
}
