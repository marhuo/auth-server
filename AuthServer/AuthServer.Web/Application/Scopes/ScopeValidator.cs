using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AuthServer.Web.Application.Scopes;

public partial class ScopeValidator(AuthDbContext db)
{

    public const int MinNameLength = 4;
    public const int MaxNameLength = 200;
    public const int MinDisplayNameLength = 4;
    public const int MaxDisplayNameLength = 200;

    [GeneratedRegex(@"^[a-z0-9][a-z0-9._:-]*\z")]
    private static partial Regex ScopeNameRegex();

    private static readonly HashSet<string> ReservedNames =
        ["openid", "profile", "email", "address", "phone", "offline_access"];

    public async Task ValidateCreateAsync(string name, string displayName, Guid? resourceServerId)
    {
        var errors = new ErrorCollector();
        ValidateName(errors, name);
        ValidateDisplayName(errors, displayName);
        errors.ThrowIfAny();

        await ValidateResourceServerAsync(resourceServerId);

        if (await db.Scopes.AnyAsync(s => s.Name == name))
            throw NameTaken();
    }

    // The scope must be loaded together with its ResourceServer.
    public async Task ValidateUpdateAsync(ScopeEntity scope, string name, string displayName, Guid? resourceServerId)
    {
        var errors = new ErrorCollector();
        ValidateDisplayName(errors, displayName);

        if (IsProtected(scope))
        {
            if (name != scope.Name)
                errors.Add("name", ErrorCode.NotAllowed,
                    "Only the display name of a standard or auth server scope can be changed.",
                    "Vakioscopen tai auth serverin scopen vain näyttönimeä voi muuttaa.");

            if (resourceServerId != scope.ResourceServerId)
                errors.Add("resourceServerId", ErrorCode.NotAllowed,
                    "Only the display name of a standard or auth server scope can be changed.",
                    "Vakioscopen tai auth serverin scopen vain näyttönimeä voi muuttaa.");

            errors.ThrowIfAny();
            return;
        }

        ValidateName(errors, name);
        errors.ThrowIfAny();

        await ValidateResourceServerAsync(resourceServerId);

        if (await db.Scopes.AnyAsync(s => s.Name == name && s.Id != scope.Id))
            throw NameTaken();
    }

    // The scope must be loaded together with its ResourceServer.
    public async Task ValidateDeleteAsync(ScopeEntity scope)
    {
        if (IsProtected(scope))
            throw new ValidationException("id", ErrorCode.NotAllowed,
                "Standard and auth server scopes cannot be deleted.",
                "Vakioscopeja ja auth serverin scopeja ei voi poistaa.");

        if (await db.Clients.AnyAsync(c => c.Scopes.Any(s => s.Id == scope.Id)))
            throw new ValidationException("id", ErrorCode.NotAllowed,
                "The scope cannot be deleted while it is assigned to clients.",
                "Scopea ei voi poistaa, kun se on annettu clienteille.");
    }

    public static ValidationException NameTaken() =>
        new("name", ErrorCode.Taken, "Name is taken.", "Nimi on jo käytössä.");

    private static bool IsProtected(ScopeEntity scope) =>
        scope.IsStandard || scope.ResourceServer!.IsAuthServer;

    private async Task ValidateResourceServerAsync(Guid? resourceServerId)
    {
        if (resourceServerId is not { } id)
            throw new ValidationException("resourceServerId", ErrorCode.Required,
                "Resource server is required.",
                "Resurssipalvelin vaaditaan.");

        var isAuthServer = await db.ResourceServers
            .Where(r => r.Id == id)
            .Select(r => (bool?)r.IsAuthServer)
            .SingleOrDefaultAsync();

        if (isAuthServer is null)
            throw new ValidationException(
                Errors.NotFound("resourceServerId", "Resource server", "Resurssipalvelinta"));

        if (isAuthServer == true)
            throw new ValidationException("resourceServerId", ErrorCode.NotAllowed,
                "Scopes cannot be added to the auth server.",
                "Auth serverille ei voi lisätä scopeja.");
    }

    private static void ValidateName(ErrorCollector errors, string name)
    {
        if (name.Length is < MinNameLength or > MaxNameLength)
        {
            errors.Add("name", ErrorCode.InvalidLength,
                $"Name must be {MinNameLength}-{MaxNameLength} characters.",
                $"Nimen on oltava {MinNameLength}–{MaxNameLength} merkkiä pitkä.");
            return;
        }

        if (!ScopeNameRegex().IsMatch(name))
        {
            errors.Add("name", ErrorCode.InvalidFormat,
                "Name may contain only lowercase letters a-z, digits and the characters . _ : -",
                "Nimi saa sisältää vain pieniä kirjaimia a–z, numeroita sekä merkit . _ : -");
            return;
        }

        if (ReservedNames.Contains(name))
        {
            errors.Add("name", ErrorCode.NotAllowed,
                "Name is reserved.",
                "Nimi on varattu.");
        }
    }

    private static void ValidateDisplayName(ErrorCollector errors, string displayName)
    {
        if (displayName.Length is < MinDisplayNameLength or > MaxDisplayNameLength)
        {
            errors.Add("displayName", ErrorCode.InvalidLength,
                $"Display name must be {MinDisplayNameLength}-{MaxDisplayNameLength} characters.",
                $"Näyttönimen on oltava {MinDisplayNameLength}–{MaxDisplayNameLength} merkkiä pitkä.");
        }
    }
}