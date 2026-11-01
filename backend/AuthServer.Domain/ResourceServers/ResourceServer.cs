using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AuthServer.Domain.ResourceServers;

public sealed partial class ResourceServer : SoftDeletableAggregateRoot
{
    public const int MinAudienceLength = 4;
    public const int MaxAudienceLength = 200;
    public const int MinDisplayNameLength = 4;
    public const int MaxDisplayNameLength = 200;

    [JsonConstructor]
    public ResourceServer(Guid id, string audience, string displayName, bool isAuthServer, DateTimeOffset createdAt, DateTimeOffset? deletedAt)
        : base(id, deletedAt)
    {
        Audience = audience;
        DisplayName = displayName;
        IsAuthServer = isAuthServer;
        CreatedAt = createdAt;
    }

    public string Audience { get; private set; }
    public string DisplayName { get; private set; }

    // The auth server's own resource server. Created by the database seeder; its audience
    // is fixed and it cannot be deleted.
    public bool IsAuthServer { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static ResourceServer Create(string audience, string displayName, DateTimeOffset now)
    {
        audience = audience.Trim();
        displayName = displayName.Trim();

        var errors = new ErrorCollector();
        ValidateAudience(errors, audience);
        ValidateDisplayName(errors, displayName);
        errors.ThrowIfAny();

        return new ResourceServer(
            id: Guid.CreateVersion7(),
            audience: audience,
            displayName: displayName,
            isAuthServer: false,
            createdAt: now,
            deletedAt: null);
    }

    public void SetAudience(string audience)
    {
        EnsureNotDeleted();
        audience = audience.Trim();

        var errors = new ErrorCollector();
        ValidateAudienceChange(errors, audience);
        errors.ThrowIfAny();

        if (audience == Audience)
            return;

        Audience = audience;
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

    public override void Delete(DateTimeOffset now)
    {
        if (IsAuthServer)
            throw new InvalidOperationException("The auth server resource server cannot be deleted.");

        base.Delete(now);
    }

    // Validates changing the audience of this resource server. Used by SetAudience and by
    // ResourceServerValidator. Expects a trimmed value.
    public void ValidateAudienceChange(ErrorCollector errors, string audience)
    {
        if (IsAuthServer)
        {
            if (audience != Audience)
                errors.Add(nameof(Audience), ErrorCode.NotAllowed,
                    "The audience of the auth server cannot be changed.",
                    "Auth serverin tunnistetta (Audience) ei voi muuttaa.");
            return;
        }

        ValidateAudience(errors, audience);
    }

    // Expects a trimmed value.
    public static void ValidateAudience(ErrorCollector errors, string audience)
    {
        if (audience.Length is < MinAudienceLength or > MaxAudienceLength || !AudienceRegex().IsMatch(audience))
            errors.Add(nameof(Audience), ErrorCode.InvalidFormat,
                $"Audience may contain only lowercase letters, digits, '.', '_' and '-' ({MinAudienceLength}-{MaxAudienceLength} characters).",
                $"Tunniste (Audience) saa sisältää vain pieniä kirjaimia, numeroita sekä merkit '.', '_' ja '-' ({MinAudienceLength}–{MaxAudienceLength} merkkiä).");
    }

    // Expects a trimmed value.
    public static void ValidateDisplayName(ErrorCollector errors, string displayName)
    {
        if (displayName.Length is < MinDisplayNameLength or > MaxDisplayNameLength)
            errors.Add(nameof(DisplayName), ErrorCode.InvalidLength,
                $"Display name must be {MinDisplayNameLength}-{MaxDisplayNameLength} characters.",
                $"Näyttönimen on oltava {MinDisplayNameLength}–{MaxDisplayNameLength} merkkiä pitkä.");
    }

    public static ValidationError AudienceTakenError() =>
        new(nameof(Audience), ErrorCode.Taken, "Audience is taken.", "Tunniste (Audience) on jo käytössä.");

    // Every product (resource server) gets a client for calling the auth server's external API,
    // created together with the resource server (ResourceServerService.CreateAsync). Nothing links the
    // client to the resource server afterwards; only the names tell whose it is.
    public static string BackendClientId(string audience) => $"{audience}-backend";

    public static string BackendClientDisplayName(string displayName) => $"{displayName} backend";

    public static ValidationError BackendClientIdTakenError(string clientId) =>
        new(nameof(Audience), ErrorCode.Taken,
            $"Client id {clientId} is taken.",
            $"Client id {clientId} on jo käytössä.");

    [GeneratedRegex(@"^[a-z0-9][a-z0-9._-]*\z")]
    private static partial Regex AudienceRegex();
}
