//AuthDbContext.cs
namespace AuthServer.Infrastructure.Persistence;

using AuthServer.Domain.Clients;
using AuthServer.Domain.ResourceServers;
using AuthServer.Domain.Roles;
using AuthServer.Domain.Scopes;
using AuthServer.Domain.Tenants;
using AuthServer.Domain.Users;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public const string RoleNameIndex = "ix_roles_name";
    public const string ResourceServerAudienceIndex = "ix_resource_servers_audience";
    public const string ScopeNameIndex = "ix_scopes_name";
    public const string ClientIdIndex = "ix_clients_client_id";
    public const string TenantSlugIndex = "ix_tenants_slug";
    public const string UserNormalizedEmailIndex = "ix_users_normalized_email";
    public const string TakenEmailIndex = "ix_taken_emails_normalized_email";

    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<ResourceServerEntity> ResourceServers => Set<ResourceServerEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<UserRoleEntity> UserRoles => Set<UserRoleEntity>();
    public DbSet<TakenEmailEntity> TakenEmails => Set<TakenEmailEntity>();
    public DbSet<ClientEntity> Clients => Set<ClientEntity>();
    public DbSet<ScopeEntity> Scopes => Set<ScopeEntity>();
    public DbSet<AuthorizationCodeEntity> AuthorizationCodes => Set<AuthorizationCodeEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<SigningKeyEntity> SigningKeys => Set<SigningKeyEntity>();
    public DbSet<PasswordTokenEntity> PasswordTokens => Set<PasswordTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.Entity<TenantEntity>(b =>
        {
            b.ToTable("tenants");
            b.HasKey(e => e.Id);

            b.Property(e => e.Name).HasMaxLength(Tenant.MaxNameLength);
            b.Property(e => e.Slug).HasMaxLength(Tenant.MaxSlugLength);
            b.Property(e => e.SuspensionReason).HasMaxLength(Tenant.MaxSuspensionReasonLength);

            b.HasIndex(e => e.Slug, TenantSlugIndex).IsUnique().HasFilter("deleted_at IS NULL");

            // Not unique: the name is a display name. The index serves sorting by name.
            b.HasIndex(e => e.NormalizedName, "ix_tenants_normalized_name")
                .HasFilter("deleted_at IS NULL");

            b.HasIndex(e => e.NormalizedName, "ix_tenants_normalized_name_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops")
                .HasFilter("deleted_at IS NULL");

            b.HasMany(e => e.UserRoles).WithOne(e => e.Tenant).HasForeignKey(e => e.TenantId);
        });

        modelBuilder.Entity<ResourceServerEntity>(b =>
        {
            b.ToTable("resource_servers", t =>
                t.HasCheckConstraint("ck_resource_servers_audience_lowercase", "audience = lower(audience)"));
            b.HasKey(e => e.Id);

            b.Property(e => e.Audience).HasMaxLength(ResourceServer.MaxAudienceLength);
            b.Property(e => e.DisplayName).HasMaxLength(ResourceServer.MaxDisplayNameLength);

            b.HasIndex(e => e.Audience, ResourceServerAudienceIndex)
                .IsUnique()
                .HasFilter("deleted_at IS NULL");

            b.HasIndex(e => e.Audience, "ix_resource_servers_audience_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops")
                .HasFilter("deleted_at IS NULL");

            b.HasIndex(e => e.DisplayName, "ix_resource_servers_display_name")
                .HasFilter("deleted_at IS NULL");

            b.HasMany(e => e.Scopes).WithOne(e => e.ResourceServer).HasForeignKey(e => e.ResourceServerId);
            b.HasMany(e => e.Roles).WithOne(e => e.ResourceServer).HasForeignKey(e => e.ResourceServerId);
            b.HasMany(e => e.Clients).WithOne(e => e.ResourceServer).HasForeignKey(e => e.ResourceServerId);
        });

        modelBuilder.Entity<UserEntity>(b =>
        {
            b.ToTable("users");
            b.HasKey(e => e.Id);

            b.Property(e => e.Email).HasMaxLength(User.MaxEmailLength);
            b.Property(e => e.NormalizedEmail).HasMaxLength(User.MaxEmailLength);

            // Uniqueness, exact lookups (login) and list order. Users are never deleted; earlier
            // emails of a user stay reserved in taken_emails.
            b.HasIndex(e => e.NormalizedEmail, UserNormalizedEmailIndex).IsUnique();

            // Substring search (ILIKE '%text%')
            b.HasIndex(e => e.NormalizedEmail, "ix_users_normalized_email_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            b.HasMany(e => e.UserRoles).WithOne(e => e.User).HasForeignKey(e => e.UserId);
            b.HasMany(e => e.TakenEmails).WithOne().HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<RoleEntity>(b =>
        {
            b.ToTable("roles", t =>
                t.HasCheckConstraint("ck_roles_name_lowercase", "name = lower(name)"));
            b.HasKey(e => e.Id);

            b.Property(e => e.Name).HasMaxLength(Role.MaxNameLength);
            b.Property(e => e.Description).HasMaxLength(Role.MaxDescriptionLength);

            b.HasIndex(e => e.Name, RoleNameIndex).IsUnique().HasFilter("deleted_at IS NULL");

            b.HasIndex(e => e.ResourceServerId, "ix_roles_resource_server_id").HasFilter("deleted_at IS NULL");

            b.HasIndex(e => e.Name, "ix_roles_name_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops")
                .HasFilter("deleted_at IS NULL");

            b.HasIndex(e => e.Description, "ix_roles_description_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops")
                .HasFilter("deleted_at IS NULL");

            b.HasMany(e => e.UserRoles).WithOne(e => e.Role).HasForeignKey(e => e.RoleId);
        });

        modelBuilder.Entity<UserRoleEntity>(b =>
        {
            b.ToTable("user_roles");
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.UserId, e.RoleId, e.TenantId }).IsUnique().HasFilter("tenant_id IS NOT NULL");
            b.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique().HasFilter("tenant_id IS NULL");
            b.HasOne(e => e.User).WithMany(e => e.UserRoles).HasForeignKey(e => e.UserId);
            b.HasOne(e => e.Role).WithMany(e => e.UserRoles).HasForeignKey(e => e.RoleId);
            b.HasOne(e => e.Tenant).WithMany(e => e.UserRoles).HasForeignKey(e => e.TenantId);

            // Matches the soft-delete filter of the required Role end, so a user role of a deleted
            // role is hidden too. Users are never deleted.
            b.HasQueryFilter(ur => ur.Role.DeletedAt == null);
        });

        modelBuilder.Entity<TakenEmailEntity>(b =>
        {
            b.ToTable("taken_emails");
            b.HasKey(e => e.Id);
            b.Property(e => e.NormalizedEmail).HasMaxLength(User.MaxEmailLength);
            b.HasIndex(e => e.NormalizedEmail, TakenEmailIndex).IsUnique();
            b.HasOne<UserEntity>().WithMany(e => e.TakenEmails).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<ClientEntity>(b =>
        {
            b.ToTable("clients");
            b.HasKey(e => e.Id);

            b.Property(e => e.ClientId).HasMaxLength(Client.MaxClientIdLength);
            b.Property(e => e.DisplayName).HasMaxLength(Client.MaxDisplayNameLength);

            b.HasIndex(e => e.ClientId, ClientIdIndex).IsUnique().HasFilter("deleted_at IS NULL");

            b.HasIndex(e => e.ClientId, "ix_clients_client_id_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops")
                .HasFilter("deleted_at IS NULL");

            b.HasIndex(e => e.DisplayName, "ix_clients_display_name_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops")
                .HasFilter("deleted_at IS NULL");

            // Array containment lookup (post-logout redirect URI check at logout).
            b.HasIndex(e => e.PostLogoutRedirectUris, "ix_clients_post_logout_redirect_uris")
                .HasMethod("gin")
                .HasFilter("deleted_at IS NULL");

            b.HasIndex(e => e.ResourceServerId, "ix_clients_resource_server_id").HasFilter("deleted_at IS NULL");

            b.HasOne(e => e.Tenant).WithMany(t => t.Clients).HasForeignKey(e => e.TenantId);
        });

        modelBuilder.Entity<ScopeEntity>(b =>
        {
            b.ToTable("scopes", t =>
                t.HasCheckConstraint("ck_scopes_name_lowercase", "name = lower(name)"));
            b.HasKey(e => e.Id);

            b.Property(e => e.Name).HasMaxLength(Scope.MaxNameLength);
            b.Property(e => e.DisplayName).HasMaxLength(Scope.MaxDisplayNameLength);

            b.HasIndex(e => e.Name, ScopeNameIndex).IsUnique().HasFilter("deleted_at IS NULL");

            b.HasIndex(e => e.ResourceServerId, "ix_scopes_resource_server_id").HasFilter("deleted_at IS NULL");

            b.HasIndex(e => e.Name, "ix_scopes_name_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops")
                .HasFilter("deleted_at IS NULL");

            b.HasIndex(e => e.DisplayName, "ix_scopes_display_name_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops")
                .HasFilter("deleted_at IS NULL");
        });

        modelBuilder.Entity<AuthorizationCodeEntity>(b =>
        {
            b.ToTable("authorization_codes");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.CodeHash).IsUnique();
            b.HasIndex(e => e.ExpiresAt);
            b.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId);
            b.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);

            // Matches the soft-delete filter of the required Client end. Users are never deleted.
            b.HasQueryFilter(c => c.Client.DeletedAt == null);
        });

        modelBuilder.Entity<RefreshTokenEntity>(b =>
        {
            b.ToTable("refresh_tokens");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.TokenHash).IsUnique();
            b.HasIndex(e => e.FamilyId);
            b.HasIndex(e => e.ExpiresAt);
            b.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId);
            b.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);

            // Matches the soft-delete filter of the required Client end. Users are never deleted.
            b.HasQueryFilter(t => t.Client.DeletedAt == null);
        });

        modelBuilder.Entity<SigningKeyEntity>(b =>
        {
            b.ToTable("signing_keys");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.KeyId).IsUnique();
            b.HasIndex(e => e.IsActive).IsUnique().HasFilter("is_active = true");
        });

        modelBuilder.Entity<PasswordTokenEntity>(b =>
        {
            b.ToTable("password_tokens");
            b.HasKey(e => e.Id);

            b.Property(e => e.Email).HasMaxLength(User.MaxEmailLength);
            b.Property(e => e.NormalizedEmail).HasMaxLength(User.MaxEmailLength);

            b.HasIndex(e => e.TokenHash).IsUnique();
            b.HasIndex(e => e.UserId);
            b.HasIndex(e => e.ExpiresAt);

            b.HasOne<UserEntity>().WithMany().HasForeignKey(e => e.UserId);
        });

        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        ApplySoftDeleteFilters(modelBuilder);
    }

    // Adds "DeletedAt == null" as a global query filter to every ISoftDeletableEntity.
    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        var softDeletable = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(ISoftDeletableEntity).IsAssignableFrom(t.ClrType))
            .Select(t => t.ClrType)
            .ToList();

        foreach (var clrType in softDeletable)
        {
            var e = Expression.Parameter(clrType, "e");
            var body = Expression.Equal(
                Expression.Property(e, nameof(ISoftDeletableEntity.DeletedAt)),
                Expression.Constant(null, typeof(DateTimeOffset?)));

            modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(body, e));
        }
    }
}

