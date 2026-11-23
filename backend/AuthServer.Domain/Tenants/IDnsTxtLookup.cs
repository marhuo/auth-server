namespace AuthServer.Domain.Tenants;

// Reads the TXT records of a domain from DNS.
public interface IDnsTxtLookup
{
    // Null if the lookup failed (e.g. a timeout); empty if the domain has no TXT records.
    Task<IReadOnlyList<string>?> GetTxtRecordsAsync(string domain);
}
