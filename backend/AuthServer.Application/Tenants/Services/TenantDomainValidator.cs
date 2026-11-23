using AuthServer.Application.Tenants.Messages;
using AuthServer.Domain.Common;
using AuthServer.Domain.Tenants;

namespace AuthServer.Application.Tenants.Services;

public class TenantDomainValidator(ITenantRepository tenants, ITenantDomainRepository domains)
{
    public async Task ValidateCreateAsync(TenantDomainCreateRequest request)
    {
        var name = TenantDomain.NormalizeName(request.Name);

        var errors = new ErrorCollector();
        TenantDomain.ValidateName(errors, name);
        errors.ThrowIfAny();

        if (!await tenants.ExistsAsync(request.TenantId))
            throw new ValidationException(Errors.NotFound(nameof(request.TenantId), "Tenant", "Asiakasta"));

        if (await domains.NameExistsInTenantAsync(request.TenantId, name))
            throw new ValidationException(TenantDomain.AddedError());

        await ThrowIfVerifiedByOtherTenantAsync(name, request.TenantId);
    }

    public Task ValidateVerifyAsync(TenantDomain domain) =>
        ThrowIfVerifiedByOtherTenantAsync(domain.Name, domain.TenantId);

    private async Task ThrowIfVerifiedByOtherTenantAsync(string name, Guid tenantId)
    {
        if (await domains.VerifiedByOtherTenantAsync(name, tenantId))
            throw new ValidationException(TenantDomain.VerifiedByOtherTenantError());
    }
}
