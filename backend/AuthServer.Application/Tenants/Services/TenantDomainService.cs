using AuthServer.Application.Tenants.Messages;
using AuthServer.Domain.Common;
using AuthServer.Domain.Tenants;

namespace AuthServer.Application.Tenants.Services;

public class TenantDomainService(
    ITenantDomainRepository domains,
    TenantDomainValidator validator,
    IDnsTxtLookup dns,
    TimeProvider timeProvider)
{
    public async Task<List<TenantDomainDto>> GetByTenantIdAsync(Guid tenantId)
    {
        var list = await domains.GetByTenantIdAsync(tenantId);
        return list.Select(TenantDomainDto.ToDto).ToList();
    }

    public async Task<TenantDomainDto> CreateAsync(TenantDomainCreateRequest request)
    {
        await validator.ValidateCreateAsync(request);

        var domain = TenantDomain.Create(
            tenantId: request.TenantId,
            name: request.Name,
            now: timeProvider.GetUtcNow());

        await domains.CreateAsync(domain);
        return TenantDomainDto.ToDto(domain);
    }

    // Verifies the domain if its DNS has the TXT record.
    public async Task<TenantDomainDto> VerifyAsync(Guid tenantId, Guid id)
    {
        var domain = await LoadAsync(tenantId, id);
        if (domain.IsVerified)
            return TenantDomainDto.ToDto(domain);

        await validator.ValidateVerifyAsync(domain);

        var txtRecords = await dns.GetTxtRecordsAsync(domain.Name)
            ?? throw new ValidationException(TenantDomain.DnsLookupFailedError());

        if (!domain.HasTxtRecord(txtRecords))
            throw new ValidationException(TenantDomain.TxtRecordNotFoundError());

        domain.MarkVerified(timeProvider.GetUtcNow());
        await domains.UpdateAsync(domain);

        return TenantDomainDto.ToDto(domain);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id)
    {
        var domain = await LoadAsync(tenantId, id);

        domain.Delete(timeProvider.GetUtcNow());

        await domains.UpdateAsync(domain);
    }

    // Checks that the verified domains still have their TXT record. Returns the number of domains that
    // lost their verification. A failed lookup changes nothing.
    public async Task<int> RecheckVerifiedAsync()
    {
        const int batchSize = 100;
        var unverifiedCount = 0;
        Guid? afterId = null;

        while (true)
        {
            var batch = await domains.GetVerifiedBatchAsync(afterId, batchSize);
            if (batch.Count == 0)
                return unverifiedCount;

            foreach (var domain in batch)
            {
                var txtRecords = await dns.GetTxtRecordsAsync(domain.Name);
                if (txtRecords is null || domain.HasTxtRecord(txtRecords))
                    continue;

                domain.MarkUnverified();
                await domains.UpdateAsync(domain);
                unverifiedCount++;
            }

            afterId = batch[^1].Id;
        }
    }

    private async Task<TenantDomain> LoadAsync(Guid tenantId, Guid id)
    {
        var domain = await domains.GetByIdAsync(id);
        return domain is not null && domain.TenantId == tenantId
            ? domain
            : throw new ValidationException(Errors.NotFound(nameof(id), "Domain", "Domainia"));
    }
}
