//AuthDbContext.cs
namespace AuthServer.Infrastructure.Persistence;

using AuthServer.Domain.Clients;
using AuthServer.Domain.ResourceServers;
using AuthServer.Domain.Roles;
using AuthServer.Domain.Scopes;
using AuthServer.Domain.Tenants;
using AuthServer.Domain.Users;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public const string RoleNameIndex = "ix_roles_name";
    public const string ResourceServerAudienceIndex = "ix_resource_servers_audience";
    public const string ScopeNameIndex = "ix_scopes_name";
    public const string ClientIdIndex = "ix_clients_client_id";
    public const string TenantSlugIndex = "ix_tenants_slug";
    public const string UserNormalizedEmailIndex = "ix_users_normalized_email";
    public const string TenantDomainVerifiedNameIndex = "ix_tenant_domains_verified_name";

    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<ResourceServerEntity> ResourceServers => Set<ResourceServerEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<UserRoleEntity> UserRoles => Set<UserRoleEntity>();
    public DbSet<UserExternalIdEntity> UserExternalIds => Set<UserExternalIdEntity>();
    public DbSet<ClientEntity> Clients => Set<ClientEntity>();
    public DbSet<ScopeEntity> Scopes => Set<ScopeEntity>();
    public DbSet<AuthorizationCodeEntity> AuthorizationCodes => Set<AuthorizationCodeEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<SigningKeyEntity> SigningKeys => Set<SigningKeyEntity>();
    public DbSet<PasswordTokenEntity> PasswordTokens => Set<PasswordTokenEntity>();
    public DbSet<TenantDomainEntity> TenantDomains => Set<TenantDomainEntity>();
    // ASP.NET Data Protection keys (Program.cs: PersistKeysToDbContext).
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

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

            b.HasIndex(e => e.Slug, TenantSlugIndex).IsUnique();

            // Not unique: the name is a display name. The index serves sorting by name.
            b.HasIndex(e => e.NormalizedName, "ix_tenants_normalized_name");

            b.HasIndex(e => e.NormalizedName, "ix_tenants_normalized_name_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            // Finding unused tenants to delete (TenantRepository.DeleteUnusedAsync).
            b.HasIndex(e => e.CreatedAt, "ix_tenants_unused_created_at").HasFilter("first_sign_in_at IS NULL");

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
                .IsUnique();

            b.HasIndex(e => e.Audience, "ix_resource_servers_audience_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            b.HasIndex(e => e.DisplayName, "ix_resource_servers_display_name");

            b.HasMany(e => e.Scopes).WithOne(e => e.ResourceServer).HasForeignKey(e => e.ResourceServerId);
            b.HasMany(e => e.Roles).WithOne(e => e.ResourceServer).HasForeignKey(e => e.ResourceServerId);
            b.HasMany(e => e.Clients).WithOne(e => e.TargetResourceServer).HasForeignKey(e => e.TargetResourceServerId);
        });

        modelBuilder.Entity<UserEntity>(b =>
        {
            b.ToTable("users");
            b.HasKey(e => e.Id);

            b.Property(e => e.Email).HasMaxLength(User.MaxEmailLength);
            b.Property(e => e.NormalizedEmail).HasMaxLength(User.MaxEmailLength);

            // Uniqueness, exact lookups (login) and list order.
            b.HasIndex(e => e.NormalizedEmail, UserNormalizedEmailIndex).IsUnique();

            // Substring search (ILIKE '%text%')
            b.HasIndex(e => e.NormalizedEmail, "ix_users_normalized_email_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            // Finding never-activated users to delete (UserRepository.DeleteUnusedInvitedAsync).
            b.HasIndex(e => new { e.Status, e.CreatedAt }, "ix_users_status_created_at");

            b.HasMany(e => e.UserRoles).WithOne(e => e.User).HasForeignKey(e => e.UserId);
            b.HasMany(e => e.ExternalIds).WithOne().HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<UserExternalIdEntity>(b =>
        {
            b.ToTable("user_external_ids");
            b.HasKey(e => e.Id);
            b.Property(e => e.Provider).HasMaxLength(50);
            b.Property(e => e.ProviderUserId).HasMaxLength(255);

            // One account of a service belongs to one user.
            b.HasIndex(e => new { e.Provider, e.ProviderUserId }, "ix_user_external_ids_provider_user").IsUnique();
            b.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<RoleEntity>(b =>
        {
            b.ToTable("roles", t =>
                t.HasCheckConstraint("ck_roles_name_lowercase", "name = lower(name)"));
            b.HasKey(e => e.Id);

            b.Property(e => e.Name).HasMaxLength(Role.MaxNameLength);
            b.Property(e => e.Description).HasMaxLength(Role.MaxDescriptionLength);

            b.HasIndex(e => e.Name, RoleNameIndex).IsUnique();

            b.HasIndex(e => e.ResourceServerId, "ix_roles_resource_server_id");

            b.HasIndex(e => e.Name, "ix_roles_name_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            b.HasIndex(e => e.Description, "ix_roles_description_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            b.HasMany(e => e.UserRoles).WithOne(e => e.Role).HasForeignKey(e => e.RoleId);
        });

        modelBuilder.Entity<UserRoleEntity>(b =>
        {
            b.ToTable("user_roles");
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.UserId, e.RoleId, e.TenantId }).IsUnique().HasFilter("tenant_id IS NOT NULL");
            b.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique().HasFilter("tenant_id IS NULL");

            // Users of a tenant (UserRepository: users with a role in the tenant). Replaces EF's automatic
            // foreign key index on tenant_id.
            b.HasIndex(e => new { e.TenantId, e.UserId }, "ix_user_roles_tenant_id_user_id");
            b.HasOne(e => e.User).WithMany(e => e.UserRoles).HasForeignKey(e => e.UserId);
            b.HasOne(e => e.Role).WithMany(e => e.UserRoles).HasForeignKey(e => e.RoleId);
            b.HasOne(e => e.Tenant).WithMany(e => e.UserRoles).HasForeignKey(e => e.TenantId);
        });


        modelBuilder.Entity<ClientEntity>(b =>
        {
            b.ToTable("clients");
            b.HasKey(e => e.Id);

            b.Property(e => e.ClientId).HasMaxLength(Client.MaxClientIdLength);
            b.Property(e => e.DisplayName).HasMaxLength(Client.MaxDisplayNameLength);

            b.HasIndex(e => e.ClientId, ClientIdIndex).IsUnique();

            b.HasIndex(e => e.ClientId, "ix_clients_client_id_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            b.HasIndex(e => e.DisplayName, "ix_clients_display_name_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            // Array containment lookup (post-logout redirect URI check at logout).
            b.HasIndex(e => e.PostLogoutRedirectUris, "ix_clients_post_logout_redirect_uris")
                .HasMethod("gin");

            b.HasIndex(e => e.TargetResourceServerId, "ix_clients_target_resource_server_id");

            b.HasOne(e => e.Tenant).WithMany(t => t.Clients).HasForeignKey(e => e.TenantId);
        });

        modelBuilder.Entity<ScopeEntity>(b =>
        {
            b.ToTable("scopes", t =>
                t.HasCheckConstraint("ck_scopes_name_lowercase", "name = lower(name)"));
            b.HasKey(e => e.Id);

            b.Property(e => e.Name).HasMaxLength(Scope.MaxNameLength);
            b.Property(e => e.DisplayName).HasMaxLength(Scope.MaxDisplayNameLength);

            b.HasIndex(e => e.Name, ScopeNameIndex).IsUnique();

            b.HasIndex(e => e.ResourceServerId, "ix_scopes_resource_server_id");

            b.HasIndex(e => e.Name, "ix_scopes_name_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            b.HasIndex(e => e.DisplayName, "ix_scopes_display_name_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");
        });

        modelBuilder.Entity<AuthorizationCodeEntity>(b =>
        {
            b.ToTable("authorization_codes");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.CodeHash).IsUnique();
            b.HasIndex(e => e.ExpiresAt);
            b.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId);
            b.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
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

        modelBuilder.Entity<TenantDomainEntity>(b =>
        {
            b.ToTable("tenant_domains");
            b.HasKey(e => e.Id);

            b.Property(e => e.Name).HasMaxLength(TenantDomain.MaxNameLength);

            b.HasIndex(e => new { e.TenantId, e.Name }, "ix_tenant_domains_tenant_id_name")
                .IsUnique();

            // A domain is verified for one tenant at a time.
            b.HasIndex(e => e.Name, TenantDomainVerifiedNameIndex)
                .IsUnique()
                .HasFilter("verified_at IS NOT NULL");

            b.HasOne<TenantEntity>().WithMany().HasForeignKey(e => e.TenantId);
        });

        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}

