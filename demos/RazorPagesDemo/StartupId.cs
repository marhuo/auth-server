namespace RazorPagesDemo;

// Changes on every start of the demo, so old sign-ins can be rejected.
public static class StartupId
{
    public const string ClaimType = "razor_pages_demo_startup";

    public static readonly string Value = Guid.NewGuid().ToString("N");
}
