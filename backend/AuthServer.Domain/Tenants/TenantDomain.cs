using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AuthServer.Domain.Tenants;

// A domain a tenant has added, e.g. "acme.fi". It is verified when the domain's DNS has the TXT record
// TxtRecord: only the owner of the domain can add it. A domain is verified for one tenant at a time.
public sealed partial class TenantDomain : AggregateRoot
{
    public const int MaxNameLength = 253;
    public const string TxtRecordPrefix = "authserver-domain-verification=";

    [JsonConstructor]
    public TenantDomain(
        Guid id,
        Guid tenantId,
        string name,
        string verificationCode,
        DateTimeOffset? verifiedAt,
        DateTimeOffset createdAt)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        VerificationCode = verificationCode;
        VerifiedAt = verifiedAt;
        CreatedAt = createdAt;
    }

    public Guid TenantId { get; private set; }

    // Lower case, without a trailing dot (NormalizeName).
    public string Name { get; private set; }

    public string VerificationCode { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    [JsonIgnore]
    public bool IsVerified => VerifiedAt is not null;

    // The TXT record the tenant adds to the domain's DNS.
    [JsonIgnore]
    public string TxtRecord => TxtRecordPrefix + VerificationCode;

    public static TenantDomain Create(Guid tenantId, string name, DateTimeOffset now)
    {
        name = NormalizeName(name);

        var errors = new ErrorCollector();
        ValidateName(errors, name);
        errors.ThrowIfAny();

        return new TenantDomain(
            id: Guid.CreateVersion7(),
            tenantId: tenantId,
            name: name,
            verificationCode: OpaqueToken.Generate(),
            verifiedAt: null,
            createdAt: now);
    }

    public bool HasTxtRecord(IEnumerable<string> txtRecords) => txtRecords.Contains(TxtRecord);

    public void MarkVerified(DateTimeOffset now)
    {
        VerifiedAt ??= now;
    }

    // The TXT record has disappeared, e.g. the domain has a new owner.
    public void MarkUnverified()
    {
        VerifiedAt = null;
    }

    // Lower case, and a domain with e.g. ä in the form DNS uses: äänekoski.fi → xn--nekoski-4waa.fi.
    public static string NormalizeName(string name)
    {
        name = name.Trim().TrimEnd('.').ToLowerInvariant();

        try
        {
            return new IdnMapping().GetAscii(name);
        }
        catch (ArgumentException)
        {
            // Not a valid domain; ValidateName rejects it.
            return name;
        }
    }

    // Expects a value normalized with NormalizeName.
    public static void ValidateName(ErrorCollector errors, string name)
    {
        if (name.Length is 0 or > MaxNameLength || !NameRegex().IsMatch(name))
            errors.Add(nameof(Name), ErrorCode.InvalidFormat,
                "Enter the domain in the form example.com.",
                "Anna domain muodossa esimerkki.fi.");
    }

    public static ValidationError AddedError() =>
        new(nameof(Name), ErrorCode.Taken, "The domain has already been added.", "Domain on jo lisätty.");

    public static ValidationError VerifiedByOtherTenantError() =>
        new(nameof(Name), ErrorCode.Taken,
            "The domain is verified for another tenant.",
            "Domain on vahvistettu toiselle asiakkaalle.");

    public static ValidationError TxtRecordNotFoundError() =>
        new(nameof(TxtRecord), ErrorCode.NotFound,
            "The TXT record was not found in the domain's DNS. DNS changes may take a while to show.",
            "TXT-riviä ei löytynyt domainin DNS:stä. DNS-muutokset voivat näkyä vasta hetken päästä.");

    public static ValidationError DnsLookupFailedError() =>
        new(nameof(Name), ErrorCode.NotFound,
            "The domain's DNS could not be read. Try again later.",
            "Domainin DNS:ää ei voitu lukea. Yritä myöhemmin uudelleen.");

    // Labels of a-z, 0-9 and hyphens, at least two labels, the last one starts with a letter.
    [GeneratedRegex(@"^([a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z]([a-z0-9-]{0,61}[a-z0-9])?\z")]
    private static partial Regex NameRegex();
}
