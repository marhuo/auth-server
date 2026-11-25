using AuthServer.Application.Users.Messages;
using AuthServer.Domain.Common;
using AuthServer.Domain.ResourceServers;
using AuthServer.Domain.Roles;
using AuthServer.Domain.Tenants;
using AuthServer.Domain.Users;

namespace AuthServer.Application.Users.Services;

public class UserValidator(
    IUserRepository users,
    IRoleRepository roles,
    IResourceServerRepository resourceServers,
    ITenantRepository tenants)
{
    public async Task ValidateInviteAsync(UserInviteRequest request)
    {
        var errors = new ErrorCollector();
        User.ValidateEmail(errors, request.Email.Trim());
        errors.ThrowIfAny();

        foreach (var role in request.UserRoles)
            await ValidateRoleAssignmentAsync(nameof(request.UserRoles), role.RoleId, role.TenantId);
    }

    // Only a user who has not signed in yet is reminded.
    public void ValidateResendInvite(User user)
    {
        if (user.Status != UserStatuses.Invited)
            throw new ValidationException("", ErrorCode.NotAllowed,
                "The user has already accepted the invitation.",
                "Käyttäjä on jo hyväksynyt kutsun.");
    }

    // A new user's email must not belong to another user.
    public async Task ValidateEmailNotTakenAsync(string email)
    {
        if (await users.EmailTakenAsync(User.NormalizeEmail(email)))
            throw new ValidationException(User.EmailTakenError());
    }

    public async Task ValidateUpdateAsync(User user, UserUpdateRequest request)
    {
        var errors = new ErrorCollector();
        User.ValidateEmail(errors, request.Email.Trim());
        errors.ThrowIfAny();

        // Locking a user must not remove the last active admin.
        if (request.Locked && !user.Locked)
            await ValidateNotLastAdminAsync(user.Id, nameof(request.Locked));

        var normalized = User.NormalizeEmail(request.Email);
        if (normalized != user.NormalizedEmail && await users.EmailTakenAsync(normalized, excludeUserId: user.Id))
            throw new ValidationException(User.EmailTakenError());
    }

    public async Task ValidateAddRoleAsync(User user, AddUserRoleRequest request)
    {
        if (user.HasRole(request.RoleId, request.TenantId))
            throw new ValidationException("", ErrorCode.NotAllowed,
                "User already has this role.",
                "Käyttäjällä on jo tämä rooli.");

        await ValidateRoleAssignmentAsync(nameof(request.RoleId), request.RoleId, request.TenantId);
    }

    // Call only when the user has the role.
    public async Task ValidateRemoveRoleAsync(User user, RemoveUserRoleRequest request)
    {
        if (!await IsAuthServerRoleAsync(request.RoleId))
            return;

        if (!await roles.HasOtherActiveHolderAsync(request.RoleId, user.Id))
            throw new ValidationException(nameof(request.RoleId), ErrorCode.NotAllowed,
                "The last holder of an auth server role cannot be removed.",
                "Auth server -roolin viimeistä haltijaa ei voi poistaa.");
    }

    // Only a product's roles can be removed this way, never the auth server's.
    public void ValidateRemoveFromTenant(ResourceServer resourceServer)
    {
        if (resourceServer.IsAuthServer)
            throw new ValidationException("ResourceServerAudience", ErrorCode.NotAllowed,
                "The auth server's roles cannot be removed this way.",
                "Auth serverin rooleja ei voi poistaa tällä tavalla.");
    }

    // Auth server roles have no tenant; other roles need an existing tenant.
    private async Task ValidateRoleAssignmentAsync(string key, Guid roleId, Guid? tenantId)
    {
        var role = await roles.GetByIdAsync(roleId)
            ?? throw new ValidationException(Errors.NotFound(key, "Role", "Roolia"));

        if (await resourceServers.IsAuthServerAsync(role.ResourceServerId) == true)
        {
            if (tenantId is not null)
                throw new ValidationException(key, ErrorCode.NotAllowed,
                    "An auth server role cannot belong to a tenant.",
                    "Auth server -rooli ei voi kuulua asiakkaalle.");

            return;
        }

        if (tenantId is not { } id)
            throw new ValidationException(key, ErrorCode.NotAllowed,
                "A tenant role needs a tenant.",
                "Asiakaskohtainen rooli vaatii asiakkaan.");

        if (!await tenants.ExistsAsync(id))
            throw new ValidationException(Errors.NotFound(key, "Tenant", "Asiakasta"));
    }

    private async Task<bool> IsAuthServerRoleAsync(Guid roleId)
    {
        var role = await roles.GetByIdAsync(roleId);
        return role is not null && await resourceServers.IsAuthServerAsync(role.ResourceServerId) == true;
    }

    // Every auth server role the user holds must have another holder who is not locked.
    private async Task ValidateNotLastAdminAsync(Guid userId, string key)
    {
        foreach (var roleId in await roles.GetAuthServerRoleIdsAsync(userId))
        {
            if (!await roles.HasOtherActiveHolderAsync(roleId, userId))
                throw new ValidationException(key, ErrorCode.NotAllowed,
                    "The last active holder of an admin role cannot be locked.",
                    "Ylläpitäjäroolin viimeistä aktiivista haltijaa ei voi lukita.");
        }
    }
}
