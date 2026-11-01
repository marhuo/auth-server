using AuthServer.Application.Common.Messages;
using AuthServer.Application.Tenants.Messages;
using AuthServer.Domain.Common;
using AuthServer.Domain.Tenants;

namespace AuthServer.Application.Tenants.Services;

public class TenantService(ITenantRepository tenants, TenantValidator validator, TimeProvider timeProvider)
{
    public async Task<TenantDto?> GetByIdAsync(Guid id)
    {
        var tenant = await tenants.GetByIdAsync(id);
        return tenant is null ? null : TenantDto.ToDto(tenant);
    }

    public async Task<List<TenantDto>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        var list = await tenants.GetByIdsAsync(ids);
        return list.Select(TenantDto.ToDto).ToList();
    }

    public async Task<PagedListDto<TenantDto>> GetPagedListAsync(PagedListRequest request)
    {
        var pagedList = await tenants.GetPagedListAsync(new PagedListQuery
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SearchTerm = request.SearchTerm
        });

        return pagedList.ToDto(TenantDto.ToDto);
    }

    // Tenants the user can sign in to through a client of the resource server.
    public async Task<List<TenantDto>> GetUserTenantsAsync(Guid userId, Guid resourceServerId)
    {
        var list = await tenants.GetUserTenantsAsync(userId, resourceServerId);
        return list.Select(TenantDto.ToDto).ToList();
    }

    public async Task<List<Guid>> GetUserTenantIdsAsync(Guid userId, Guid resourceServerId)
    {
        var list = await tenants.GetUserTenantsAsync(userId, resourceServerId);
        return list.Select(t => t.Id).ToList();
    }

    public async Task<Guid> CreateAsync(TenantCreateRequest request)
    {
        validator.ValidateCreate(request);

        var tenant = Tenant.Create(
            name: request.Name,
            slug: await GetFreeSlugAsync(request.Name),
            now: timeProvider.GetUtcNow());

        await tenants.CreateAsync(tenant);
        return tenant.Id;
    }

    // Two concurrent creates with the same name may still pick the same slug; the unique index then
    // rejects the second one with a "slug is taken" error.
    private async Task<string> GetFreeSlugAsync(string name)
    {
        const int maxAttempts = 1000;

        for (var number = 1; number <= maxAttempts; number++)
        {
            var slug = Tenant.SlugFromName(name.Trim(), number);
            if (!await tenants.SlugExistsAsync(slug))
                return slug;
        }

        throw new InvalidOperationException($"No free slug found for tenant name '{name}'.");
    }

    public async Task UpdateAsync(TenantUpdateRequest request)
    {
        var tenant = await LoadAsync(request.Id);

        await validator.ValidateUpdateAsync(tenant, request);

        tenant.SetName(request.Name);
        tenant.SetSlug(request.Slug);

        await tenants.UpdateAsync(tenant);
    }

    public async Task SuspendAsync(TenantSuspendRequest request)
    {
        validator.ValidateSuspend(request);

        var tenant = await LoadAsync(request.Id);

        tenant.Suspend(request.SuspensionReason, timeProvider.GetUtcNow());

        await tenants.UpdateAsync(tenant);
    }

    public async Task UnsuspendAsync(Guid id)
    {
        var tenant = await LoadAsync(id);

        tenant.Unsuspend();

        await tenants.UpdateAsync(tenant);
    }

    public async Task DeleteAsync(Guid id)
    {
        var tenant = await LoadAsync(id);

        await validator.ValidateDeleteAsync(tenant);

        tenant.Delete(timeProvider.GetUtcNow());

        await tenants.UpdateAsync(tenant);
    }

    private async Task<Tenant> LoadAsync(Guid id) =>
        await tenants.GetByIdAsync(id)
        ?? throw new ValidationException(Errors.NotFound(nameof(id), "Tenant", "Asiakasta"));
}
