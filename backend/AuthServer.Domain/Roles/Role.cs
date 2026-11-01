using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AuthServer.Domain.Roles;

public sealed partial class Role : SoftDeletableAggregateRoot
{
    public const int MinNameLength = 2;
    public const int MaxNameLength = 64;
    public const int MaxDescriptionLength = 300;

    [JsonConstructor]
    public Role(Guid id, Guid resourceServerId, string name, string description, DateTimeOffset createdAt, DateTimeOffset? deletedAt)
        : base(id, deletedAt)
    {
        ResourceServerId = resourceServerId;
        Name = name;
        Description = description;
        CreatedAt = createdAt;
    }

    public Guid ResourceServerId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Role Create(Guid resourceServerId, string name, string description, DateTimeOffset now)
    {
        name = name.Trim();
        description = description.Trim();

        var errors = new ErrorCollector();
        ValidateName(errors, name);
        ValidateDescription(errors, description);
        errors.ThrowIfAny();

        return new Role(
            id: Guid.CreateVersion7(),
            resourceServerId: resourceServerId,
            name: name,
            description: description,
            createdAt: now,
            deletedAt: null);
    }

    public void SetName(string name)
    {
        EnsureNotDeleted();
        name = name.Trim();

        var errors = new ErrorCollector();
        ValidateName(errors, name);
        errors.ThrowIfAny();

        if (name == Name)
            return;

        Name = name;
    }

    public void SetDescription(string description)
    {
        EnsureNotDeleted();
        description = description.Trim();

        var errors = new ErrorCollector();
        ValidateDescription(errors, description);
        errors.ThrowIfAny();

        if (description == Description)
            return;

        Description = description;
    }

    public void SetResourceServerId(Guid resourceServerId)
    {
        EnsureNotDeleted();

        if (resourceServerId == ResourceServerId)
            return;

        ResourceServerId = resourceServerId;
    }

    // Used both by the aggregate and by RoleValidator, so the rules exist in one place.
    // Expects a trimmed value.
    public static void ValidateName(ErrorCollector errors, string name)
    {
        if (name.Length is < MinNameLength or > MaxNameLength)
        {
            errors.Add(nameof(Name), ErrorCode.InvalidLength,
                $"Name must be {MinNameLength}-{MaxNameLength} characters.",
                $"Nimen on oltava {MinNameLength}–{MaxNameLength} merkkiä pitkä.");
            return;
        }

        if (!NameRegex().IsMatch(name))
        {
            errors.Add(nameof(Name), ErrorCode.InvalidFormat,
                "Name may contain only lowercase letters a-z, digits and the characters . _ : -",
                "Nimi saa sisältää vain pieniä kirjaimia a–z, numeroita sekä merkit . _ : -");
        }
    }

    // Expects a trimmed value.
    public static void ValidateDescription(ErrorCollector errors, string description)
    {
        if (description.Length > MaxDescriptionLength)
        {
            errors.Add(nameof(Description), ErrorCode.InvalidLength,
                $"Description must be at most {MaxDescriptionLength} characters.",
                $"Kuvauksen on oltava enintään {MaxDescriptionLength} merkkiä pitkä.");
        }
    }

    // Lookup form of a role name, e.g. for names coming from an invitation.
    public static string NormalizeName(string name) => name.Trim().ToLowerInvariant();

    public static ValidationError NameTakenError() =>
        new(nameof(Name), ErrorCode.Taken, "Name is taken.", "Nimi on jo käytössä.");

    [GeneratedRegex(@"^[a-z0-9][a-z0-9._:-]*\z")]
    private static partial Regex NameRegex();
}
