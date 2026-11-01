using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AuthServer.Domain.Scopes;

public sealed partial class Scope : SoftDeletableAggregateRoot
{
    public const int MinNameLength = 4;
    public const int MaxNameLength = 200;
    public const int MinDisplayNameLength = 4;
    public const int MaxDisplayNameLength = 200;

    // OpenID Connect standard scope names. They are created by the database seeder.
    private static readonly HashSet<string> ReservedNames =
        ["openid", "profile", "email", "address", "phone", "offline_access"];

    [JsonConstructor]
    public Scope(Guid id, string name, string displayName, Guid? resourceServerId, bool isStandard, DateTimeOffset createdAt, DateTimeOffset? deletedAt)
        : base(id, deletedAt)
    {
        Name = name;
        DisplayName = displayName;
        ResourceServerId = resourceServerId;
        IsStandard = isStandard;
        CreatedAt = createdAt;
    }

    public string Name { get; private set; }
    public string DisplayName { get; private set; }

    // Null for global scopes, which are not tied to a resource server.
    public Guid? ResourceServerId { get; private set; }

    // OpenID Connect standard scope (openid, profile, ...). Only the display name can be changed.
    public bool IsStandard { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Scope Create(Guid resourceServerId, string name, string displayName, DateTimeOffset now)
    {
        name = name.Trim();
        displayName = displayName.Trim();

        var errors = new ErrorCollector();
        ValidateName(errors, name);
        ValidateDisplayName(errors, displayName);
        errors.ThrowIfAny();

        return new Scope(
            id: Guid.CreateVersion7(),
            name: name,
            displayName: displayName,
            resourceServerId: resourceServerId,
            isStandard: false,
            createdAt: now,
            deletedAt: null);
    }

    public void SetName(string name)
    {
        EnsureNotDeleted();
        name = name.Trim();

        if (name == Name)
            return;

        EnsureNotStandard();

        var errors = new ErrorCollector();
        ValidateName(errors, name);
        errors.ThrowIfAny();

        Name = name;
    }

    public void SetDisplayName(string displayName)
    {
        EnsureNotDeleted();
        displayName = displayName.Trim();

        var errors = new ErrorCollector();
        ValidateDisplayName(errors, displayName);
        errors.ThrowIfAny();

        if (displayName == DisplayName)
            return;

        DisplayName = displayName;
    }

    public void SetResourceServerId(Guid resourceServerId)
    {
        EnsureNotDeleted();

        if (resourceServerId == ResourceServerId)
            return;

        EnsureNotStandard();

        ResourceServerId = resourceServerId;
    }

    public override void Delete(DateTimeOffset now)
    {
        EnsureNotStandard();
        base.Delete(now);
    }

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
            return;
        }

        if (ReservedNames.Contains(name))
        {
            errors.Add(nameof(Name), ErrorCode.NotAllowed,
                "Name is reserved.",
                "Nimi on varattu.");
        }
    }

    // Expects a trimmed value.
    public static void ValidateDisplayName(ErrorCollector errors, string displayName)
    {
        if (displayName.Length is < MinDisplayNameLength or > MaxDisplayNameLength)
        {
            errors.Add(nameof(DisplayName), ErrorCode.InvalidLength,
                $"Display name must be {MinDisplayNameLength}-{MaxDisplayNameLength} characters.",
                $"Näyttönimen on oltava {MinDisplayNameLength}–{MaxDisplayNameLength} merkkiä pitkä.");
        }
    }

    public static ValidationError NameTakenError() =>
        new(nameof(Name), ErrorCode.Taken, "Name is taken.", "Nimi on jo käytössä.");

    private void EnsureNotStandard()
    {
        if (IsStandard)
            throw new InvalidOperationException($"Standard scope {Name} can only have its display name changed.");
    }

    [GeneratedRegex(@"^[a-z0-9][a-z0-9._:-]*\z")]
    private static partial Regex NameRegex();
}
