using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AuthServer.Web.Application.ResourceServers;

public partial class ResourceServerValidator(AuthDbContext db)
{
    private const int MinDisplayNameLength = 4;
    private const int MaxDisplayNameLength = 200;

    [GeneratedRegex(@"^[a-z0-9][a-z0-9._-]{3,199}\z")]
    private static partial Regex AudienceRegex();

    public async Task ValidateCreateAsync(string audience, string displayName)
    {
        var errors = new ErrorCollector();
        ValidateAudience(errors, audience);
        ValidateDisplayName(errors, displayName);
        errors.ThrowIfAny();

        if (await db.ResourceServers.AnyAsync(rs => rs.Audience == audience))
            throw AudienceTaken();
    }

    public async Task ValidateUpdateAsync(ResourceServerEntity resourceServer, string audience, string displayName)
    {
        var errors = new ErrorCollector();

        if (resourceServer.IsAuthServer)
        {
            if (audience != resourceServer.Audience)
                errors.Add("audience", ErrorCode.NotAllowed,
                    "The audience of the auth server cannot be changed.",
                    "Auth serverin tunnistetta (Audience) ei voi muuttaa.");
        }
        else
        {
            ValidateAudience(errors, audience);
        }

        ValidateDisplayName(errors, displayName);
        errors.ThrowIfAny();

        if (audience != resourceServer.Audience
            && await db.ResourceServers.AnyAsync(rs => rs.Audience == audience && rs.Id != resourceServer.Id))
            throw AudienceTaken();
    }

    private static void ValidateAudience(ErrorCollector errors, string audience)
    {
        if (!AudienceRegex().IsMatch(audience))
            errors.Add("audience", ErrorCode.InvalidFormat,
                "Audience may contain only lowercase letters, digits, '.', '_' and '-' (4-200 characters).",
                "Tunniste (Audience) saa sisältää vain pieniä kirjaimia, numeroita sekä merkit '.', '_' ja '-' (4–200 merkkiä).");
    }

    public async Task ValidateDeleteAsync(ResourceServerEntity resourceServer)
    {
        if (resourceServer.IsAuthServer)
            throw new ValidationException("id", ErrorCode.NotAllowed,
                "The auth server cannot be deleted.",
                "Auth serveriä ei voi poistaa.");

        var id = resourceServer.Id;
        var errors = new ErrorCollector();

        if (await db.Roles.AnyAsync(r => r.ResourceServerId == id))
            errors.Add("id", ErrorCode.NotAllowed,
                "The resource server cannot be deleted while it has roles.",
                "Resurssipalvelinta ei voi poistaa, kun sillä on rooleja.");

        if (await db.Scopes.AnyAsync(s => s.ResourceServerId == id))
            errors.Add("id", ErrorCode.NotAllowed,
                "The resource server cannot be deleted while it has scopes.",
                "Resurssipalvelinta ei voi poistaa, kun sillä on scopeja.");

        if (await db.Clients.AnyAsync(c => c.ResourceServerId == id))
            errors.Add("id", ErrorCode.NotAllowed,
                "The resource server cannot be deleted while it has clients.",
                "Resurssipalvelinta ei voi poistaa, kun sillä on clientteja.");

        errors.ThrowIfAny();
    }

    public static ValidationException AudienceTaken() =>
        new("audience", ErrorCode.Taken, "Audience is taken.", "Tunniste (Audience) on jo käytössä.");

    private static void ValidateDisplayName(ErrorCollector errors, string displayName)
    {
        if (displayName.Length is < MinDisplayNameLength or > MaxDisplayNameLength)
            errors.Add("displayName", ErrorCode.InvalidLength,
                $"Display name must be {MinDisplayNameLength}-{MaxDisplayNameLength} characters.",
                $"Näyttönimen on oltava {MinDisplayNameLength}–{MaxDisplayNameLength} merkkiä pitkä.");
    }
}