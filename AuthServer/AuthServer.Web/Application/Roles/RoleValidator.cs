using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AuthServer.Web.Application.Roles;

public partial class RoleValidator(AuthDbContext db)
{
    public const int MinNameLength = 2;
    public const int MaxNameLength = 64;
    public const int MaxDescriptionLength = 300;

    [GeneratedRegex(@"^[a-z0-9][a-z0-9._:-]*\z")]
    private static partial Regex RoleNameRegex();

    public async Task ValidateCreateAsync(string name, string description, Guid resourceServerId)
    {
        var errors = new ErrorCollector();
        ValidateName(errors, name);
        ValidateDescription(errors, description);
        errors.ThrowIfAny();

        await ValidateResourceServerAsync(resourceServerId);

        if (await db.Roles.AnyAsync(r => r.Name == name))
            throw NameTaken();
    }

    // The role must be loaded together with its ResourceServer.
    public async Task ValidateUpdateAsync(RoleEntity role, string name, string description, Guid resourceServerId)
    {
        var errors = new ErrorCollector();
        ValidateName(errors, name);
        ValidateDescription(errors, description);
        errors.ThrowIfAny();

        if (role.ResourceServer.IsAuthServer)
            throw AuthServerRolesAreFixed();

        await ValidateResourceServerAsync(resourceServerId);

        var assigned = await db.UserRoles.AnyAsync(ur => ur.RoleId == role.Id);

        if (assigned && role.ResourceServerId != resourceServerId)
            throw new ValidationException("resourceServerId", ErrorCode.NotAllowed,
                "The resource server cannot be changed while the role is assigned to users.",
                "Resurssipalvelinta ei voi vaihtaa, kun rooli on annettu käyttäjille.");

        if (assigned && role.Name != name)
            throw new ValidationException("name", ErrorCode.NotAllowed,
                "The name cannot be changed while the role is assigned to users.",
                "Nimeä ei voi vaihtaa, kun rooli on annettu käyttäjille.");

        if (await db.Roles.AnyAsync(r => r.Name == name && r.Id != role.Id))
            throw NameTaken();
    }

    // The role must be loaded together with its ResourceServer.
    public async Task ValidateDeleteAsync(RoleEntity role)
    {
        if (role.ResourceServer.IsAuthServer)
            throw AuthServerRolesAreFixed("id");

        if (await db.UserRoles.AnyAsync(ur => ur.RoleId == role.Id))
            throw new ValidationException("id", ErrorCode.NotAllowed,
                "The role cannot be deleted while it is assigned to users.",
                "Roolia ei voi poistaa, kun se on annettu käyttäjille.");
    }

    public static ValidationException NameTaken() =>
        new("name", ErrorCode.Taken, "Name is taken.", "Nimi on jo käytössä.");

    private async Task ValidateResourceServerAsync(Guid resourceServerId)
    {
        var isAuthServer = await db.ResourceServers
            .Where(r => r.Id == resourceServerId)
            .Select(r => (bool?)r.IsAuthServer)
            .SingleOrDefaultAsync();

        if (isAuthServer is null)
            throw new ValidationException(
                Errors.NotFound("resourceServerId", "Resource server", "Resurssipalvelinta"));

        if (isAuthServer == true)
            throw AuthServerRolesAreFixed();
    }

    private static ValidationException AuthServerRolesAreFixed(string key = "resourceServerId") =>
        new(key, ErrorCode.NotAllowed,
            "Roles of the auth server cannot be created, changed or deleted.",
            "Auth serverin rooleja ei voi luoda, muokata tai poistaa.");

    private static void ValidateName(ErrorCollector errors, string name)
    {
        if (name.Length is < MinNameLength or > MaxNameLength)
        {
            errors.Add("name", ErrorCode.InvalidLength,
                $"Name must be {MinNameLength}-{MaxNameLength} characters.",
                $"Nimen on oltava {MinNameLength}–{MaxNameLength} merkkiä pitkä.");
            return;
        }

        if (!RoleNameRegex().IsMatch(name))
        {
            errors.Add("name", ErrorCode.InvalidFormat,
                "Name may contain only lowercase letters a-z, digits and the characters . _ : -",
                "Nimi saa sisältää vain pieniä kirjaimia a–z, numeroita sekä merkit . _ : -");
        }
    }

    private static void ValidateDescription(ErrorCollector errors, string description)
    {
        if (description.Length > MaxDescriptionLength)
        {
            errors.Add("description", ErrorCode.InvalidLength,
                $"Description must be at most {MaxDescriptionLength} characters.",
                $"Kuvauksen on oltava enintään {MaxDescriptionLength} merkkiä pitkä.");
        }
    }
}