using AuthServer.Application.Common.Messages;
using AuthServer.Application.Roles.Messages;
using AuthServer.Domain.Common;
using AuthServer.Domain.Roles;

namespace AuthServer.Application.Roles.Services;

public class RoleService(IRoleRepository roles, RoleValidator validator, TimeProvider timeProvider)
{
    public async Task<RoleDto?> GetByIdAsync(Guid id)
    {
        var role = await roles.GetByIdAsync(id);
        return role is null ? null : RoleDto.ToDto(role);
    }

    public async Task<List<RoleDto>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        var list = await roles.GetByIdsAsync(ids);
        return list.Select(RoleDto.ToDto).ToList();
    }

    // Keys are the given names (blank names skipped); the value is null when the resource server
    // has no role with that name.
    public async Task<Dictionary<string, Guid?>> GetIdsByNamesAsync(Guid resourceServerId, IReadOnlyCollection<string> names)
    {
        var normalizedByName = names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToDictionary(n => n, Role.NormalizeName);

        var found = await roles.GetIdsByNamesAsync(resourceServerId, normalizedByName.Values.Distinct().ToList());

        return normalizedByName.ToDictionary(
            p => p.Key,
            p => found.TryGetValue(p.Value, out var id) ? id : (Guid?)null);
    }

    public async Task<PagedListDto<RoleDto>> GetPagedListAsync(RolePagedListRequest request)
    {
        var pagedList = await roles.GetPagedListAsync(new RolePagedListQuery
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SearchTerm = request.SearchTerm,
            ResourceServerId = request.ResourceServerId
        });

        return pagedList.ToDto(RoleDto.ToDto);
    }

    public async Task<Guid> CreateAsync(RoleCreateRequest request)
    {
        await validator.ValidateCreateAsync(request);

        var role = Role.Create(
            resourceServerId: request.ResourceServerId,
            name: request.Name,
            description: request.Description,
            now: timeProvider.GetUtcNow());

        await roles.CreateAsync(role);
        return role.Id;
    }

    public async Task UpdateAsync(RoleUpdateRequest request)
    {
        var role = await roles.GetByIdAsync(request.Id)
            ?? throw new ValidationException(Errors.NotFound(nameof(request.Id), "Role", "Roolia"));

        await validator.ValidateUpdateAsync(role, request);

        role.SetName(request.Name);
        role.SetDescription(request.Description);
        role.SetResourceServerId(request.ResourceServerId);

        await roles.UpdateAsync(role);
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await roles.GetByIdAsync(id)
            ?? throw new ValidationException(Errors.NotFound(nameof(id), "Role", "Roolia"));

        await validator.ValidateDeleteAsync(role);

        role.Delete(timeProvider.GetUtcNow());

        await roles.UpdateAsync(role);
    }
}
