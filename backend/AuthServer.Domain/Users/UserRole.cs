namespace AuthServer.Domain.Users;

// A role assigned to a user. TenantId is null for auth server roles and set for tenant roles.
public sealed record UserRole(Guid RoleId, Guid? TenantId, DateTimeOffset CreatedAt);
