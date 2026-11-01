using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AuthServer.Web.Application.Tenants;

public partial class TenantValidator(AuthDbContext db)
{
    private const int MinNameLength = 1;
    private const int MaxNameLength = 200;
    private const int MaxSlugLength = 63;
    private const int MaxSuspensionReasonLength = 500;

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*\z")]
    private static partial Regex SlugRegex();

    private static readonly HashSet<string> ReservedSlugs = ["admin", "connect", "login", "logout", "api"];

    public async Task ValidateCreateAsync(string name, string slug)
    {
        var errors = new ErrorCollector();
        ValidateName(errors, name);
        ValidateSlug(errors, slug);
        errors.ThrowIfAny();

        await ThrowIfTakenAsync(TenantService.NormalizeName(name), slug);
    }

    public async Task ValidateUpdateAsync(Guid id, string name, string slug)
    {
        var errors = new ErrorCollector();
        ValidateName(errors, name);
        ValidateSlug(errors, slug);
        errors.ThrowIfAny();

        await ThrowIfTakenAsync(TenantService.NormalizeName(name), slug, id);
    }

    public void ValidateSuspend(string reason)
    {
        var errors = new ErrorCollector();

        if (reason.Length is 0 or > MaxSuspensionReasonLength)
            errors.Add("suspensionReason", ErrorCode.InvalidLength,
                $"Suspension reason must be 1-{MaxSuspensionReasonLength} characters.",
                $"Keskeytyksen syyn on oltava 1–{MaxSuspensionReasonLength} merkkiä pitkä.");

        errors.ThrowIfAny();
    }

    public async Task ValidateDeleteAsync(Guid id)
    {
        var errors = new ErrorCollector();

        if (await db.Clients.AnyAsync(c => c.TenantId == id))
            errors.Add("id", ErrorCode.NotAllowed,
                "The tenant cannot be deleted while it has clients.",
                "Asiakasta ei voi poistaa, kun sillä on clientteja.");

        if (await db.UserRoles.AnyAsync(ur => ur.TenantId == id))
            errors.Add("id", ErrorCode.NotAllowed,
                "The tenant cannot be deleted while users have roles in it.",
                "Asiakasta ei voi poistaa, kun käyttäjillä on siinä rooleja.");

        errors.ThrowIfAny();
    }

    public async Task ThrowIfTakenAsync(string normalizedName, string slug, Guid? exceptId = null)
    {
        var except = exceptId ?? Guid.Empty;

        var nameTaken = await db.Tenants.AnyAsync(t => t.NormalizedName == normalizedName && t.Id != except);
        var slugTaken = await db.Tenants.AnyAsync(t => t.Slug == slug && t.Id != except);

        if (!nameTaken && !slugTaken)
            return;

        var errors = new List<ValidationError>();

        if (nameTaken)
            errors.Add(new ValidationError("name", ErrorCode.Taken,
                "Name is taken.", "Nimi on jo käytössä."));

        if (slugTaken)
            errors.Add(new ValidationError("slug", ErrorCode.Taken,
                "Slug is taken.", "Slug on jo käytössä."));

        throw new ValidationException(errors);
    }

    private static void ValidateName(ErrorCollector errors, string name)
    {
        if (name.Length is < MinNameLength or > MaxNameLength)
            errors.Add("name", ErrorCode.InvalidLength,
                $"Name must be {MinNameLength}-{MaxNameLength} characters.",
                $"Nimen on oltava {MinNameLength}–{MaxNameLength} merkkiä pitkä.");
    }

    private static void ValidateSlug(ErrorCollector errors, string slug)
    {
        if (slug.Length is 0 or > MaxSlugLength)
        {
            errors.Add("slug", ErrorCode.InvalidLength,
                $"Slug must be 1-{MaxSlugLength} characters.",
                $"Url-tunnisteen(slug) on oltava 1–{MaxSlugLength} merkkiä pitkä.");
            return;
        }

        if (!SlugRegex().IsMatch(slug))
        {
            errors.Add("slug", ErrorCode.InvalidFormat,
                "Slug may contain only a-z, 0-9 and single hyphens.",
                "Url-tunniste(slug) saa sisältää vain pieniä kirjaimia a–z, numeroita ja yksittäisiä yhdysmerkkejä.");
            return;
        }

        if (ReservedSlugs.Contains(slug))
            errors.Add("slug", ErrorCode.NotAllowed,
                "Slug is reserved.",
                "Url-tunniste(slug) on varattu.");
    }
}