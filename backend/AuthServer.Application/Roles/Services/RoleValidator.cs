using AuthServer.Application.Roles.Messages;
using AuthServer.Domain.Common;
using AuthServer.Domain.ResourceServers;
using AuthServer.Domain.Roles;

namespace AuthServer.Application.Roles.Services;

public class RoleValidator(IRoleRepository roles, IResourceServerRepository resourceServers)
{
    public async Task ValidateCreateAsync(RoleCreateRequest request)
    {
        var name = request.Name.Trim();

        ValidateInput(name, request.Description.Trim());

        await ValidateResourceServerAsync(request.ResourceServerId);

        if (await roles.NameExistsAsync(name))
            throw new ValidationException(Role.NameTakenError());
    }

    public async Task ValidateUpdateAsync(Role role, RoleUpdateRequest request)
    {
        var name = request.Name.Trim();

        ValidateInput(name, request.Description.Trim());

        if (await resourceServers.IsAuthServerAsync(role.ResourceServerId) == true)
            throw AuthServerRolesAreFixed(nameof(RoleUpdateRequest.ResourceServerId));

        await ValidateResourceServerAsync(request.ResourceServerId);

        var assigned = await roles.IsAssignedToUsersAsync(role.Id);

        if (assigned && role.ResourceServerId != request.ResourceServerId)
            throw new ValidationException(nameof(RoleUpdateRequest.ResourceServerId), ErrorCode.NotAllowed,
                "The resource server cannot be changed while the role is assigned to users.",
                "Resurssipalvelinta ei voi vaihtaa, kun rooli on annettu käyttäjille.");

        if (assigned && role.Name != name)
            throw new ValidationException(nameof(RoleUpdateRequest.Name), ErrorCode.NotAllowed,
                "The name cannot be changed while the role is assigned to users.",
                "Nimeä ei voi vaihtaa, kun rooli on annettu käyttäjille.");

        if (await roles.NameExistsAsync(name, excludeId: role.Id))
            throw new ValidationException(Role.NameTakenError());
    }

    public async Task ValidateDeleteAsync(Role role)
    {
        if (await resourceServers.IsAuthServerAsync(role.ResourceServerId) == true)
            throw AuthServerRolesAreFixed("id");

        if (await roles.IsAssignedToUsersAsync(role.Id))
            throw new ValidationException("id", ErrorCode.NotAllowed,
                "The role cannot be deleted while it is assigned to users.",
                "Roolia ei voi poistaa, kun se on annettu käyttäjille.");
    }

    // Input errors are thrown before any check that needs the database.
    private static void ValidateInput(string name, string description)
    {
        var errors = new ErrorCollector();
        Role.ValidateName(errors, name);
        Role.ValidateDescription(errors, description);
        errors.ThrowIfAny();
    }

    private async Task ValidateResourceServerAsync(Guid resourceServerId)
    {
        var isAuthServer = await resourceServers.IsAuthServerAsync(resourceServerId);

        if (isAuthServer is null)
            throw new ValidationException(
                Errors.NotFound(nameof(RoleCreateRequest.ResourceServerId), "Resource server", "Resurssipalvelinta"));

        if (isAuthServer == true)
            throw AuthServerRolesAreFixed(nameof(RoleCreateRequest.ResourceServerId));
    }

    private static ValidationException AuthServerRolesAreFixed(string key) =>
        new(key, ErrorCode.NotAllowed,
            "Roles of the auth server cannot be created, changed or deleted.",
            "Auth serverin rooleja ei voi luoda, muokata tai poistaa.");
}
