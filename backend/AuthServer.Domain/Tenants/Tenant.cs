using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AuthServer.Domain.Tenants;

// Deleted physically, and only if nobody has signed in to it (FirstSignInAt).
public sealed partial class Tenant : AggregateRoot
{
    public const int MinNameLength = 1;
    public const int MaxNameLength = 200;
    public const int MaxSlugLength = 63;
    public const int MaxSuspensionReasonLength = 500;

    // Slugs that would collide with routes of the auth server.
    private static readonly HashSet<string> ReservedSlugs = ["admin", "connect", "login", "logout", "api"];

    [JsonConstructor]
    public Tenant(
        Guid id,
        string name,
        string normalizedName,
        string slug,
        DateTimeOffset? suspendedAt,
        string? suspensionReason,
        DateTimeOffset? firstSignInAt,
        DateTimeOffset createdAt)
        : base(id)
    {
        Name = name;
        NormalizedName = normalizedName;
        Slug = slug;
        SuspendedAt = suspendedAt;
        SuspensionReason = suspensionReason;
        FirstSignInAt = firstSignInAt;
        CreatedAt = createdAt;
    }

    public string Name { get; private set; }

    // Search and sort key, see NormalizeName. The name is a display name and is not unique: different
    // customers may have the same name. The slug is unique.
    public string NormalizedName { get; private set; }

    // Short url-friendly name.
    public string Slug { get; private set; }

    public DateTimeOffset? SuspendedAt { get; private set; }
    public string? SuspensionReason { get; private set; }

    // When a user first signed in to an app inside this tenant. Null: the tenant has never been used.
    public DateTimeOffset? FirstSignInAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    [JsonIgnore]
    public bool IsSuspended => SuspendedAt is not null;

    // The slug is derived from the name with SlugFromName (the service picks a free one); it can be
    // changed later with SetSlug.
    public static Tenant Create(string name, string slug, DateTimeOffset now)
    {
        name = name.Trim();

        var errors = new ErrorCollector();
        ValidateName(errors, name);
        ValidateSlug(errors, slug);
        errors.ThrowIfAny();

        return new Tenant(
            id: Guid.CreateVersion7(),
            name: name,
            normalizedName: NormalizeName(name),
            slug: slug,
            suspendedAt: null,
            suspensionReason: null,
            firstSignInAt: null,
            createdAt: now);
    }

    // Returns false if somebody has signed in before: then nothing changes.
    public bool MarkSignedIn(DateTimeOffset now)
    {
        if (FirstSignInAt is not null)
            return false;

        FirstSignInAt = now;
        return true;
    }

    public static ValidationError UsedTenantDeleteError() =>
        new("id", ErrorCode.NotAllowed,
            "The tenant cannot be deleted because users have signed in to it.",
            "Asiakasta ei voi poistaa, koska siihen on kirjauduttu.");

    public void SetName(string name)
    {
        name = name.Trim();

        var errors = new ErrorCollector();
        ValidateName(errors, name);
        errors.ThrowIfAny();

        if (name == Name)
            return;

        Name = name;
        NormalizedName = NormalizeName(name);
    }

    public void SetSlug(string slug)
    {
        slug = NormalizeSlug(slug);

        var errors = new ErrorCollector();
        ValidateSlug(errors, slug);
        errors.ThrowIfAny();

        if (slug == Slug)
            return;

        Slug = slug;
    }

    // Suspending an already suspended tenant only updates the reason.
    public void Suspend(string reason, DateTimeOffset now)
    {
        reason = reason.Trim();

        var errors = new ErrorCollector();
        ValidateSuspensionReason(errors, reason);
        errors.ThrowIfAny();

        SuspendedAt ??= now;
        SuspensionReason = reason;
    }

    public void Unsuspend()
    {
        SuspendedAt = null;
        SuspensionReason = null;
    }

    // Expects a trimmed value.
    public static void ValidateName(ErrorCollector errors, string name)
    {
        if (name.Length is < MinNameLength or > MaxNameLength)
            errors.Add(nameof(Name), ErrorCode.InvalidLength,
                $"Name must be {MinNameLength}-{MaxNameLength} characters.",
                $"Nimen on oltava {MinNameLength}–{MaxNameLength} merkkiä pitkä.");
    }

    // Expects a value normalized with NormalizeSlug or created with SlugFromName.
    public static void ValidateSlug(ErrorCollector errors, string slug)
    {
        if (slug.Length is 0 or > MaxSlugLength)
        {
            errors.Add(nameof(Slug), ErrorCode.InvalidLength,
                $"Slug must be 1-{MaxSlugLength} characters.",
                $"Url-tunnisteen(slug) on oltava 1–{MaxSlugLength} merkkiä pitkä.");
            return;
        }

        if (!SlugRegex().IsMatch(slug))
        {
            errors.Add(nameof(Slug), ErrorCode.InvalidFormat,
                "Slug may contain only a-z, 0-9 and single hyphens.",
                "Url-tunniste(slug) saa sisältää vain pieniä kirjaimia a–z, numeroita ja yksittäisiä yhdysmerkkejä.");
            return;
        }

        if (ReservedSlugs.Contains(slug))
            errors.Add(nameof(Slug), ErrorCode.NotAllowed,
                "Slug is reserved.",
                "Url-tunniste(slug) on varattu.");
    }

    // Expects a trimmed value.
    public static void ValidateSuspensionReason(ErrorCollector errors, string reason)
    {
        if (reason.Length is 0 or > MaxSuspensionReasonLength)
            errors.Add(nameof(SuspensionReason), ErrorCode.InvalidLength,
                $"Suspension reason must be 1-{MaxSuspensionReasonLength} characters.",
                $"Keskeytyksen syyn on oltava 1–{MaxSuspensionReasonLength} merkkiä pitkä.");
    }

    public static string NormalizeName(string name) =>
        name.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();

    public static string NormalizeSlug(string slug) => slug.Trim().ToLowerInvariant();

    // Several tenants may have the same name, but slugs are unique, so a new tenant gets the first free
    // one of "name", "name-2", "name-3", ... A long name is cut so that the number fits.
    public static string SlugFromName(string name, int number = 1)
    {
        var slug = Common.Slug.From(name);
        var suffix = number == 1 ? "" : $"-{number}";

        if (slug.Length + suffix.Length > MaxSlugLength)
            slug = slug[..(MaxSlugLength - suffix.Length)].TrimEnd('-');

        return slug + suffix;
    }

    public static ValidationError SlugTakenError() =>
        new(nameof(Slug), ErrorCode.Taken, "Slug is taken.", "Slug on jo käytössä.");

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*\z")]
    private static partial Regex SlugRegex();
}
