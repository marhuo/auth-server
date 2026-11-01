namespace Muistio;

// Changes on every start of Muistio. Every sign-in gets it as a claim, and sign-ins with another
// value (from before the restart) are rejected (Program.cs).
public static class StartupId
{
    public const string ClaimType = "muistio_startup";

    public static readonly string Value = Guid.NewGuid().ToString("N");
}
