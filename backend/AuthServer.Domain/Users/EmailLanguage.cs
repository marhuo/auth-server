namespace AuthServer.Domain.Users;

// The language of the emails sent to a user.
public enum EmailLanguage { Fi, En }

public static class EmailLanguages
{
    // "en", "en-US", ... is English; anything else, also null, is Finnish.
    public static EmailLanguage FromCode(string? code) =>
        code?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true ? EmailLanguage.En : EmailLanguage.Fi;
}
