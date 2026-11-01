using AuthServer.Application.Common.Messages;
using AuthServer.Application.ResourceServers.Messages;
using AuthServer.Domain.Common;
using AuthServer.Domain.ResourceServers;

namespace AuthServer.Application.ResourceServers.Services;

public class ResourceServerService(
    IResourceServerRepository resourceServers,
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

    public async Task<Guid> CreateAsync(ResourceServerCreateRequest request)
    {
        await validator.ValidateCreateAsync(request);

        var resourceServer = ResourceServer.Create(
            audience: request.Audience,
            displayName: request.DisplayName,
            now: timeProvider.GetUtcNow());

        await resourceServers.CreateAsync(resourceServer);
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

        resourceServer.Delete(timeProvider.GetUtcNow());

        await resourceServers.UpdateAsync(resourceServer);
    }

    private static ValidationException NotFound(string key) =>
        new(Errors.NotFound(key, "Resource server", "Resurssipalvelinta"));
}
