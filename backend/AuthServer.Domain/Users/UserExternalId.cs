namespace AuthServer.Domain.Users;

// The user's id in an external sign-in service, e.g. Google's "sub". It never changes, unlike the email.
public sealed record UserExternalId(string Provider, string ProviderUserId, DateTimeOffset CreatedAt);

public static class ExternalProviders
{
    public const string Google = "google";
}

// What an external sign-in service says about the user.
public sealed record ExternalIdentity(string Provider, string ProviderUserId, string Email, bool EmailVerified);
