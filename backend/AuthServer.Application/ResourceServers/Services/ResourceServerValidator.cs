using AuthServer.Application.ResourceServers.Messages;
using AuthServer.Domain.Clients;
using AuthServer.Domain.Common;
using AuthServer.Domain.ResourceServers;
using AuthServer.Domain.Roles;
using AuthServer.Domain.Scopes;

namespace AuthServer.Application.ResourceServers.Services;

public class ResourceServerValidator(
    IResourceServerRepository resourceServers,
    IRoleRepository roles,
    IScopeRepository scopes,
    IClientRepository clients)
{
    public async Task ValidateCreateAsync(ResourceServerCreateRequest request)
    {
        var audience = request.Audience.Trim();

        var displayName = request.DisplayName.Trim();

        var errors = new ErrorCollector();
        ResourceServer.ValidateAudience(errors, audience);
        ResourceServer.ValidateDisplayName(errors, displayName);
        errors.ThrowIfAny();

        // The backend client (ResourceServerService.CreateAsync). Its scope is checked by the service.
        var clientId = ResourceServer.BackendClientId(audience);
        Client.ValidateCreate(
            errors, ClientTypes.ConfidentialMachine, clientId, ResourceServer.BackendClientDisplayName(displayName),
            request.Secret, [], [], []);
        errors.ThrowIfAny();

        if (await resourceServers.AudienceExistsAsync(audience))
            throw new ValidationException(ResourceServer.AudienceTakenError());

        if (await clients.ClientIdExistsAsync(clientId))
            throw new ValidationException(ResourceServer.BackendClientIdTakenError(clientId));
    }

    public async Task ValidateUpdateAsync(ResourceServer resourceServer, ResourceServerUpdateRequest request)
    {
        var audience = request.Audience.Trim();

        var errors = new ErrorCollector();
        resourceServer.ValidateAudienceChange(errors, audience);
        ResourceServer.ValidateDisplayName(errors, request.DisplayName.Trim());
        errors.ThrowIfAny();

        if (audience != resourceServer.Audience
            && await resourceServers.AudienceExistsAsync(audience, excludeId: resourceServer.Id))
            throw new ValidationException(ResourceServer.AudienceTakenError());
    }

    public async Task ValidateDeleteAsync(ResourceServer resourceServer)
    {
        if (resourceServer.IsAuthServer)
            throw new ValidationException("id", ErrorCode.NotAllowed,
                "The auth server cannot be deleted.",
                "Auth serveriä ei voi poistaa.");

        var id = resourceServer.Id;
        var errors = new ErrorCollector();

        if (await roles.AnyForResourceServerAsync(id))
            errors.Add("id", ErrorCode.NotAllowed,
                "The resource server cannot be deleted while it has roles.",
                "Resurssipalvelinta ei voi poistaa, kun sillä on rooleja.");

        if (await scopes.AnyForResourceServerAsync(id))
            errors.Add("id", ErrorCode.NotAllowed,
                "The resource server cannot be deleted while it has scopes.",
                "Resurssipalvelinta ei voi poistaa, kun sillä on scopeja.");

        if (await clients.AnyForTargetResourceServerAsync(id))
            errors.Add("id", ErrorCode.NotAllowed,
                "The resource server cannot be deleted while it has clients.",
                "Resurssipalvelinta ei voi poistaa, kun sillä on clientteja.");

        errors.ThrowIfAny();
    }
}
