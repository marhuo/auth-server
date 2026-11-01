using AuthServer.Application.Tenants.Messages;
using AuthServer.Domain.Clients;
using AuthServer.Domain.Common;
using AuthServer.Domain.Roles;
using AuthServer.Domain.Tenants;

namespace AuthServer.Application.Tenants.Services;

public class TenantValidator(ITenantRepository tenants, IClientRepository clients, IRoleRepository roles)
{
    // A taken slug is not an error here: the service numbers it (Tenant.SlugFromName).
    public void ValidateCreate(TenantCreateRequest request)
    {
        var name = request.Name.Trim();

        var errors = new ErrorCollector();
        Tenant.ValidateName(errors, name);
        errors.ThrowIfAny();

        // E.g. a name with no letters or digits gives an empty slug.
        Tenant.ValidateSlug(errors, Tenant.SlugFromName(name));
        errors.ThrowIfAny();
    }

    public async Task ValidateUpdateAsync(Tenant tenant, TenantUpdateRequest request)
    {
        var name = request.Name.Trim();
        var slug = Tenant.NormalizeSlug(request.Slug);

        var errors = new ErrorCollector();
        Tenant.ValidateName(errors, name);
        Tenant.ValidateSlug(errors, slug);
        errors.ThrowIfAny();

        await ThrowIfSlugTakenAsync(slug, excludeId: tenant.Id);
    }

    public void ValidateSuspend(TenantSuspendRequest request)
    {
        var errors = new ErrorCollector();
        Tenant.ValidateSuspensionReason(errors, request.SuspensionReason.Trim());
        errors.ThrowIfAny();
    }

    public async Task ValidateDeleteAsync(Tenant tenant)
    {
        var errors = new ErrorCollector();

        if (await clients.AnyForTenantAsync(tenant.Id))
            errors.Add("id", ErrorCode.NotAllowed,
                "The tenant cannot be deleted while it has clients.",
                "Asiakasta ei voi poistaa, kun sillä on clientteja.");

        if (await roles.AnyRoleInTenantAsync(tenant.Id))
            errors.Add("id", ErrorCode.NotAllowed,
                "The tenant cannot be deleted while users have roles in it.",
                "Asiakasta ei voi poistaa, kun käyttäjillä on siinä rooleja.");

        errors.ThrowIfAny();
    }

    // Only the slug is unique; the name is a display name that several tenants may share.
    private async Task ThrowIfSlugTakenAsync(string slug, Guid? excludeId)
    {
        if (await tenants.SlugExistsAsync(slug, excludeId))
            throw new ValidationException(Tenant.SlugTakenError());
    }
}
