using AuthServer.Domain.Tenants;
using DnsClient;
using Microsoft.Extensions.Logging;

namespace AuthServer.Infrastructure.Tenants;

public class DnsTxtLookup(ILogger<DnsTxtLookup> logger) : IDnsTxtLookup
{
    // No cache: a record the tenant just added must be seen at once.
    private static readonly LookupClient Client = new(new LookupClientOptions
    {
        UseCache = false,
        Timeout = TimeSpan.FromSeconds(5),
        Retries = 2
    });

    public async Task<IReadOnlyList<string>?> GetTxtRecordsAsync(string domain)
    {
        try
        {
            var response = await Client.QueryAsync(domain, QueryType.TXT);

            // A domain that does not exist has no records; other errors are failures.
            if (response.HasError && response.Header.ResponseCode != DnsHeaderResponseCode.NotExistentDomain)
            {
                logger.LogWarning("TXT lookup of {Domain:l} failed: {Error:l}", domain, response.ErrorMessage);
                return null;
            }

            // A long record is split into several strings; they form one value.
            return response.Answers.TxtRecords().Select(r => string.Concat(r.Text)).ToList();
        }
        catch (DnsResponseException e)
        {
            logger.LogWarning(e, "TXT lookup of {Domain:l} failed", domain);
            return null;
        }
    }
}
