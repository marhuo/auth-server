using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace AuthServer.Domain.Users;

// A six-digit code sent by email for signing in without a password. Bound to the email, not to a
// user: the user is created only when the code is correct. Only a hash is stored. An email has at most
// one usable code: creating a new one invalidates the earlier ones (see ISignInCodeRepository.CreateAsync).
public sealed class SignInCode : AggregateRoot
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
    public const int MaxAttempts = 10;

    [JsonConstructor]
    public SignInCode(
        Guid id,
        string normalizedEmail,
        string codeHash,
        int failedAttempts,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        DateTimeOffset? usedAt)
        : base(id)
    {
        NormalizedEmail = normalizedEmail;
        CodeHash = codeHash;
        FailedAttempts = failedAttempts;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        UsedAt = usedAt;
    }

    public string NormalizedEmail { get; private set; }
    public string CodeHash { get; private set; }
    public int FailedAttempts { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    // Set when the code is used or invalidated.
    public DateTimeOffset? UsedAt { get; private set; }

    // Returns the aggregate and the code that is sent to the user.
    public static (SignInCode Code, string RawCode) Create(string email, DateTimeOffset now)
    {
        var rawCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var id = Guid.CreateVersion7();

        var code = new SignInCode(
            id: id,
            normalizedEmail: User.NormalizeEmail(email),
            codeHash: HashCode(id, rawCode),
            failedAttempts: 0,
            createdAt: now,
            expiresAt: now + Lifetime,
            usedAt: null);

        return (code, rawCode);
    }

    public bool IsUsable(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;

    // Returns true and uses the code if it is correct. A wrong code counts as an attempt; after
    // MaxAttempts the code is invalidated, so it cannot be guessed.
    public bool Verify(string rawCode, DateTimeOffset now)
    {
        if (!IsUsable(now))
            return false;

        if (CodeHash == HashCode(Id, rawCode.Trim()))
        {
            UsedAt = now;
            return true;
        }

        FailedAttempts++;
        if (FailedAttempts >= MaxAttempts)
            UsedAt = now;

        return false;
    }

    // The id is included so that equal codes have different hashes.
    private static string HashCode(Guid id, string rawCode) => OpaqueToken.Hash($"{id}:{rawCode}");
}
