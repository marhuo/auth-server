using AuthServer.Application.Clients.Messages;
using AuthServer.Application.Common.Messages;
using AuthServer.Application.Scopes.Messages;
using AuthServer.Domain.Clients;
using AuthServer.Domain.Common;
using AuthServer.Domain.Scopes;

namespace AuthServer.Application.Scopes.Services;

public class ScopeService(IScopeRepository scopes, ScopeValidator validator, TimeProvider timeProvider)
{
    public async Task<ScopeDto?> GetByIdAsync(Guid id)
    {
        var scope = await scopes.GetByIdAsync(id);
        return scope is null ? null : ScopeDto.ToDto(scope);
    }

    public async Task<List<ScopeDto>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        var list = await scopes.GetByIdsAsync(ids);
        return list.Select(ScopeDto.ToDto).ToList();
    }

    public async Task<PagedListDto<ScopeDto>> GetPagedListAsync(ScopePagedListRequest request)
    {
        var pagedList = await scopes.GetPagedListAsync(new ScopePagedListQuery
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SearchTerm = request.SearchTerm,
            ResourceServerId = request.ResourceServerId
        });

        return pagedList.ToDto(ScopeDto.ToDto);
    }

    // Scopes a client of the given type can be granted: the resource server's own scopes,
    // plus the global scopes for clients that sign in users.
    public async Task<List<ScopeDto>> GetSelectableForClientAsync(Guid resourceServerId, ClientDtoTypes clientType)
    {
        var serverScopes = await scopes.GetByResourceServerIdAsync(resourceServerId);

        var selectable = clientType.ToDomain().SignsInUsers
            ? (await scopes.GetGlobalAsync()).Concat(serverScopes)
            : serverScopes;

        return selectable.Select(ScopeDto.ToDto).ToList();
    }

    // Ids of the existing scopes with the given names. Used by the OAuth endpoints.
    public Task<List<Guid>> GetIdsByNamesAsync(IReadOnlyCollection<string> names) =>
        scopes.GetIdsByNamesAsync(names);

    public async Task<Guid> CreateAsync(ScopeCreateRequest request)
    {
        await validator.ValidateCreateAsync(request);

        var scope = Scope.Create(
            resourceServerId: request.ResourceServerId!.Value,
            name: request.Name,
            displayName: request.DisplayName,
            now: timeProvider.GetUtcNow());

        await scopes.CreateAsync(scope);
        return scope.Id;
    }

    public async Task UpdateAsync(ScopeUpdateRequest request)
    {
        var scope = await scopes.GetByIdAsync(request.Id)
            ?? throw NotFound(nameof(request.Id));

        await validator.ValidateUpdateAsync(scope, request);

        scope.SetName(request.Name);
        scope.SetDisplayName(request.DisplayName);

        // Null only for global scopes, whose resource server cannot change (checked by the validator).
        if (request.ResourceServerId is { } resourceServerId)
            scope.SetResourceServerId(resourceServerId);

        await scopes.UpdateAsync(scope);
    }

    public async Task DeleteAsync(Guid id)
    {
        var scope = await scopes.GetByIdAsync(id)
            ?? throw NotFound(nameof(id));

        await validator.ValidateDeleteAsync(scope);

        scope.Delete(timeProvider.GetUtcNow());

        await scopes.UpdateAsync(scope);
    }

    private static ValidationException NotFound(string key) =>
        new(Errors.NotFound(key, "Scope", "Scopea"));
}
