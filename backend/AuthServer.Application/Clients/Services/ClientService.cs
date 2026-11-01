using AuthServer.Application.Clients.Messages;
using AuthServer.Application.Common.Messages;
using AuthServer.Domain.Clients;
using AuthServer.Domain.Common;

namespace AuthServer.Application.Clients.Services;

public class ClientService(IClientRepository clients, ClientValidator validator, TimeProvider timeProvider)
{
    public async Task<ClientDto?> GetByIdAsync(Guid id)
    {
        var client = await clients.GetByIdAsync(id);
        return client is null ? null : ClientDto.ToDto(client);
    }

    public async Task<ClientDto?> GetByClientIdAsync(string clientId)
    {
        var client = await clients.GetByClientIdAsync(clientId);
        return client is null ? null : ClientDto.ToDto(client);
    }

    public async Task<List<ClientDto>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        var list = await clients.GetByIdsAsync(ids);
        return list.Select(ClientDto.ToDto).ToList();
    }

    public async Task<PagedListDto<ClientDto>> GetPagedListAsync(PagedListRequest request)
    {
        var pagedList = await clients.GetPagedListAsync(new PagedListQuery
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SearchTerm = request.SearchTerm
        });

        return pagedList.ToDto(ClientDto.ToDto);
    }

    public async Task<Guid> CreateAsync(ClientCreateRequest request)
    {
        await validator.ValidateCreateAsync(request);

        // Token lifetimes are not part of the request yet: new clients get the defaults.
        var type = request.Type.ToDomain();

        var client = Client.Create(
            type: type,
            clientId: request.ClientId,
            displayName: request.DisplayName,
            secret: request.Secret,
            targetResourceServerId: request.TargetResourceServerId,
            tenantId: request.TenantId,
            redirectUris: request.RedirectUris,
            postLogoutRedirectUris: request.PostLogoutRedirectUris,
            scopeIds: request.ScopeIds,
            accessTokenLifetime: Client.DefaultAccessTokenLifetime,
            idTokenLifetime: type.SignsInUsers ? Client.DefaultIdTokenLifetime : null,
            refreshTokenLifetime: type.SignsInUsers ? Client.DefaultRefreshTokenLifetime : null,
            now: timeProvider.GetUtcNow());

        await clients.CreateAsync(client);
        return client.Id;
    }

    public async Task UpdateAsync(ClientUpdateRequest request)
    {
        var client = await LoadAsync(request.Id);

        await validator.ValidateUpdateAsync(client, request);

        client.SetClientId(request.ClientId);
        client.SetDisplayName(request.DisplayName);
        client.SetTenantId(request.TenantId);
        client.SetInitiateLoginUri(request.InitiateLoginUri);

        await clients.UpdateAsync(client);
    }

    public async Task DeleteAsync(Guid id)
    {
        var client = await LoadAsync(id);

        client.Delete(timeProvider.GetUtcNow());

        await clients.UpdateAsync(client);
    }

    public async Task DeactivateAsync(Guid id)
    {
        var client = await LoadAsync(id);

        client.Deactivate();

        await clients.UpdateAsync(client);
    }

    public async Task ChangeSecretAsync(ClientChangeSecretRequest request)
    {
        var client = await LoadAsync(request.Id);

        validator.ValidateChangeSecret(client, request);

        client.ChangeSecret(request.NewSecret);

        await clients.UpdateAsync(client);
    }

    public async Task AddScopeAsync(ClientAddScopeRequest request)
    {
        var client = await LoadAsync(request.Id);

        await validator.ValidateAddScopeAsync(client, request);

        client.AddScope(request.ScopeId);

        await clients.UpdateAsync(client);
    }

    public async Task RemoveScopeAsync(ClientRemoveScopeRequest request)
    {
        var client = await LoadAsync(request.Id);

        validator.ValidateRemoveScope(client, request);

        client.RemoveScope(request.ScopeId);

        await clients.UpdateAsync(client);
    }

    public async Task AddRedirectUriAsync(ClientAddRedirectUriRequest request)
    {
        var client = await LoadAsync(request.Id);

        validator.ValidateAddRedirectUri(client, request);

        client.AddRedirectUri(request.Uri);

        await clients.UpdateAsync(client);
    }

    public async Task RemoveRedirectUriAsync(ClientRemoveRedirectUriRequest request)
    {
        var client = await LoadAsync(request.Id);

        validator.ValidateRemoveRedirectUri(client, request);

        client.RemoveRedirectUri(request.Uri);

        await clients.UpdateAsync(client);
    }

    public async Task AddPostLogoutRedirectUriAsync(ClientAddPostLogoutRedirectUriRequest request)
    {
        var client = await LoadAsync(request.Id);

        validator.ValidateAddPostLogoutRedirectUri(client, request);

        client.AddPostLogoutRedirectUri(request.Uri);

        await clients.UpdateAsync(client);
    }

    public async Task RemovePostLogoutRedirectUriAsync(ClientRemovePostLogoutRedirectUriRequest request)
    {
        var client = await LoadAsync(request.Id);

        client.RemovePostLogoutRedirectUri(request.Uri);

        await clients.UpdateAsync(client);
    }

    private async Task<Client> LoadAsync(Guid id) =>
        await clients.GetByIdAsync(id)
        ?? throw new ValidationException(Errors.NotFound(nameof(id), "Client", "Clientia"));
}
