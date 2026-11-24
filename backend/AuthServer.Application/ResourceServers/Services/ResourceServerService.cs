using AuthServer.Application.Common.Messages;
using AuthServer.Application.ResourceServers.Messages;
using AuthServer.Domain.Clients;
using AuthServer.Domain.Common;
using AuthServer.Domain.ResourceServers;
using AuthServer.Domain.Scopes;

namespace AuthServer.Application.ResourceServers.Services;

public class ResourceServerService(
    IResourceServerRepository resourceServers,
    IClientRepository clients,
    IScopeRepository scopes,
    ResourceServerValidator validator,
    TimeProvider timeProvider)
{
    public async Task<ResourceServerDto?> GetByIdAsync(Guid id)
    {
        var resourceServer = await resourceServers.GetByIdAsync(id);
        return resourceServer is null ? null : ResourceServerDto.ToDto(resourceServer);
    }

    public async Task<List<ResourceServerDto>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        var list = await resourceServers.GetByIdsAsync(ids);
        return list.Select(ResourceServerDto.ToDto).ToList();
    }

    public async Task<List<ResourceServerDto>> GetAllAsync()
    {
        var list = await resourceServers.GetAllAsync();
        return list.Select(ResourceServerDto.ToDto).ToList();
    }

    public async Task<PagedListDto<ResourceServerDto>> GetPagedListAsync(PagedListRequest request)
    {
        var pagedList = await resourceServers.GetPagedListAsync(new PagedListQuery
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SearchTerm = request.SearchTerm
        });

        return pagedList.ToDto(ResourceServerDto.ToDto);
    }

    // Also creates the resource server's backend client: a machine client whose target is the auth
    // server, with the scope auth_server:api, so that the product can create tenants and invite users
    // through the external API. Giving the right roles there is the product's responsibility.
    public async Task<Guid> CreateAsync(ResourceServerCreateRequest request)
    {
        await validator.ValidateCreateAsync(request);

        var now = timeProvider.GetUtcNow();

        var resourceServer = ResourceServer.Create(
            audience: request.Audience,
            displayName: request.DisplayName,
            now: now);

        var authServer = await resourceServers.GetAuthServerAsync();
        var apiScopeIds = await scopes.GetIdsByNamesAsync([SystemScopes.AuthServerApi]);
        if (apiScopeIds.Count != 1)
            throw new InvalidOperationException($"The scope {SystemScopes.AuthServerApi} was not found.");

        var backendClient = Client.Create(
            type: ClientTypes.ConfidentialMachine,
            clientId: ResourceServer.BackendClientId(resourceServer.Audience),
            displayName: ResourceServer.BackendClientDisplayName(resourceServer.DisplayName),
            secret: request.Secret,
            targetResourceServerId: authServer.Id,
            tenantId: null,
            redirectUris: null,
            postLogoutRedirectUris: null,
            scopeIds: apiScopeIds,
            accessTokenLifetime: Client.DefaultAccessTokenLifetime,
            idTokenLifetime: null,
            refreshTokenLifetime: null,
            now: now);

        await resourceServers.CreateAsync(resourceServer);
        await clients.CreateAsync(backendClient);
        return resourceServer.Id;
    }

    public async Task UpdateAsync(ResourceServerUpdateRequest request)
    {
        var resourceServer = await resourceServers.GetByIdAsync(request.Id)
            ?? throw NotFound(nameof(request.Id));

        await validator.ValidateUpdateAsync(resourceServer, request);

        resourceServer.SetAudience(request.Audience);
        resourceServer.SetDisplayName(request.DisplayName);

        await resourceServers.UpdateAsync(resourceServer);
    }

    public async Task DeleteAsync(Guid id)
    {
        var resourceServer = await resourceServers.GetByIdAsync(id)
            ?? throw NotFound(nameof(id));

        await validator.ValidateDeleteAsync(resourceServer);

        await resourceServers.DeleteAsync(resourceServer.Id);
    }

    private static ValidationException NotFound(string key) =>
        new(Errors.NotFound(key, "Resource server", "Resurssipalvelinta"));
}
