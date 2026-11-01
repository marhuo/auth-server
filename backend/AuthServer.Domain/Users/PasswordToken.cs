using System.Text.Json.Serialization;

namespace AuthServer.Domain.Users;

// Invitation: sent when a user is invited; the user sets their first password.
// Reset: sent when the user has forgotten their password.
public enum PasswordTokenTypes { Invitation, Reset }

// A single-use link that lets a user set their password. Only the hash of the token is stored; the
// raw token is sent to the user by email. A user has at most one usable token: creating a new one
// invalidates the earlier unused ones (see IPasswordTokenRepository.CreateAsync).
public sealed class PasswordToken : AggregateRoot
{
    public static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);
    public static readonly TimeSpan ResetLifetime = TimeSpan.FromHours(1);

    [JsonConstructor]
    public PasswordToken(
        Guid id,
        Guid userId,
        PasswordTokenTypes type,
        string email,
        string normalizedEmail,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        DateTimeOffset? usedAt)
        : base(id)
    {
        UserId = userId;
        Type = type;
        Email = email;
        NormalizedEmail = normalizedEmail;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        UsedAt = usedAt;
    }

    public Guid UserId { get; private set; }
    public PasswordTokenTypes Type { get; private set; }

    // The address the link was sent to.
    public string Email { get; private set; }

    public string NormalizedEmail { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    // Returns the aggregate and the raw token that is sent to the user.
    public static (PasswordToken Token, string RawToken) Create(
        Guid userId, string email, PasswordTokenTypes type, DateTimeOffset now)
    {
        var rawToken = OpaqueToken.Generate();
        var lifetime = type == PasswordTokenTypes.Invitation ? InvitationLifetime : ResetLifetime;

        var token = new PasswordToken(
            id: Guid.CreateVersion7(),
            userId: userId,
            type: type,
            email: email,
            normalizedEmail: User.NormalizeEmail(email),
            tokenHash: HashToken(rawToken),
            createdAt: now,
            expiresAt: now + lifetime,
            usedAt: null);

        return (token, rawToken);
    }

    public static string HashToken(string rawToken) => OpaqueToken.Hash(rawToken);

    // Whether the link can still be used to set the password: used only once, before it expires.
    public bool IsUsable(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;

    public void MarkUsed(DateTimeOffset now)
    {
        if (!IsUsable(now))
            throw new InvalidOperationException($"Password token {Id} is already used or expired.");

        UsedAt = now;
    }
}
