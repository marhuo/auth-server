namespace AuthServer.Application.Users.Messages;

public class UserRemoveFromTenantRequest
{
    public required Guid UserId { get; init; }
    public required Guid TenantId { get; init; }

    // The product whose roles are removed, e.g. "razor-pages-demo". Other products' roles stay.
    public required string ResourceServerAudience { get; init; }
}
