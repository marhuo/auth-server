using AuthServer.Application.Scopes.Messages;
using AuthServer.Domain.Common;
using AuthServer.Domain.ResourceServers;
using AuthServer.Domain.Scopes;

namespace AuthServer.Application.Scopes.Services;

public class ScopeValidator(IScopeRepository scopes, IResourceServerRepository resourceServers)
{
    private const string ProtectedEn = "Only the display name of a standard or auth server scope can be changed.";
    private const string ProtectedFi = "Vakioscopen tai auth serverin scopen vain näyttönimeä voi muuttaa.";

    public async Task ValidateCreateAsync(ScopeCreateRequest request)
    {
        var name = request.Name.Trim();

        var errors = new ErrorCollector();
        Scope.ValidateName(errors, name);
        Scope.ValidateDisplayName(errors, request.DisplayName.Trim());
        ValidateResourceServerRequired(errors, request.ResourceServerId);
        errors.ThrowIfAny();

        await ValidateResourceServerAsync(request.ResourceServerId!.Value);

        if (await scopes.NameExistsAsync(name))
            throw new ValidationException(Scope.NameTakenError());
    }

    public async Task ValidateUpdateAsync(Scope scope, ScopeUpdateRequest request)
    {
        var name = request.Name.Trim();

        // Depends only on the stored scope, not on the input.
        var isProtected = await IsProtectedAsync(scope);

        var errors = new ErrorCollector();
        Scope.ValidateDisplayName(errors, request.DisplayName.Trim());

        if (isProtected)
        {
            if (name != scope.Name)
                errors.Add(nameof(ScopeUpdateRequest.Name), ErrorCode.NotAllowed, ProtectedEn, ProtectedFi);

            if (request.ResourceServerId != scope.ResourceServerId)
                errors.Add(nameof(ScopeUpdateRequest.ResourceServerId), ErrorCode.NotAllowed, ProtectedEn, ProtectedFi);

            errors.ThrowIfAny();
            return;
        }

        Scope.ValidateName(errors, name);
        ValidateResourceServerRequired(errors, request.ResourceServerId);
        errors.ThrowIfAny();

        await ValidateResourceServerAsync(request.ResourceServerId!.Value);

        if (await scopes.NameExistsAsync(name, excludeId: scope.Id))
            throw new ValidationException(Scope.NameTakenError());
    }

    public async Task ValidateDeleteAsync(Scope scope)
    {
        if (await IsProtectedAsync(scope))
            throw new ValidationException("id", ErrorCode.NotAllowed,
                "Standard and auth server scopes cannot be deleted.",
                "Vakioscopeja ja auth serverin scopeja ei voi poistaa.");

        if (await scopes.IsAssignedToClientsAsync(scope.Id))
            throw new ValidationException("id", ErrorCode.NotAllowed,
                "The scope cannot be deleted while it is assigned to clients.",
                "Scopea ei voi poistaa, kun se on annettu clienteille.");
    }

    // Standard scopes and the auth server's own scopes can only have their display name changed.
    private async Task<bool> IsProtectedAsync(Scope scope) =>
        scope.IsStandard
        || (scope.ResourceServerId is { } id && await resourceServers.IsAuthServerAsync(id) == true);

    private static void ValidateResourceServerRequired(ErrorCollector errors, Guid? resourceServerId)
    {
        if (resourceServerId is null)
            errors.Add(nameof(ScopeCreateRequest.ResourceServerId), ErrorCode.Required,
                "Resource server is required.",
                "Resurssipalvelin vaaditaan.");
    }

    private async Task ValidateResourceServerAsync(Guid resourceServerId)
    {
        var isAuthServer = await resourceServers.IsAuthServerAsync(resourceServerId);

        if (isAuthServer is null)
            throw new ValidationException(
                Errors.NotFound(nameof(ScopeCreateRequest.ResourceServerId), "Resource server", "Resurssipalvelinta"));

        if (isAuthServer == true)
            throw new ValidationException(nameof(ScopeCreateRequest.ResourceServerId), ErrorCode.NotAllowed,
                "Scopes cannot be added to the auth server.",
                "Auth serverille ei voi lisätä scopeja.");
    }
}
