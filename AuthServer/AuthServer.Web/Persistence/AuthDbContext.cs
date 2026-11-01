//AuthDbContext.cs
namespace AuthServer.Web.Persistence;

using AuthServer.Web.Application.Roles;
using AuthServer.Web.Application.Scopes;
using Microsoft.EntityFrameworkCore;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<ResourceServerEntity> ResourceServers => Set<ResourceServerEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<UserRoleEntity> UserRoles => Set<UserRoleEntity>();
    public DbSet<TakenUsernameEntity> TakenUsernames => Set<TakenUsernameEntity>();
    public DbSet<ClientEntity> Clients => Set<ClientEntity>();
    public DbSet<ScopeEntity> Scopes => Set<ScopeEntity>();
    public DbSet<AuthorizationCodeEntity> AuthorizationCodes => Set<AuthorizationCodeEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<SigningKeyEntity> SigningKeys => Set<SigningKeyEntity>();
    public DbSet<UserTokenEntity> UserTokens => Set<UserTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.Entity<TenantEntity>(b =>
        {
            b.ToTable("tenants");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.Slug).IsUnique();

            b.HasIndex(e => e.NormalizedName, "ix_tenants_normalized_name")
                .IsUnique();

            b.HasIndex(e => e.NormalizedName, "ix_tenants_normalized_name_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            b.HasMany(e => e.UserRoles).WithOne(e => e.Tenant).HasForeignKey(e => e.TenantId);
        });

        modelBuilder.Entity<ResourceServerEntity>(b =>
        {
            b.ToTable("resource_servers", t =>
                t.HasCheckConstraint("ck_resource_servers_audience_lowercase", "audience = lower(audience)"));
            b.HasKey(e => e.Id);

            b.HasIndex(e => e.Audience, "ix_resource_servers_audience")
                .IsUnique();

            b.HasIndex(e => e.Audience, "ix_resource_servers_audience_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            b.HasMany(e => e.Scopes).WithOne(e => e.ResourceServer).HasForeignKey(e => e.ResourceServerId);
            b.HasMany(e => e.Roles).WithOne(e => e.ResourceServer).HasForeignKey(e => e.ResourceServerId);
            b.HasMany(e => e.Clients).WithOne(e => e.ResourceServer).HasForeignKey(e => e.ResourceServerId);
        });

        modelBuilder.Entity<UserEntity>(b =>
        {
            b.ToTable("users");
            b.HasKey(e => e.Id);

            // Uniqueness and exact lookups
            b.HasIndex(e => e.NormalizedUserName, "ix_users_normalized_user_name")
                .IsUnique();
            b.HasIndex(e => e.NormalizedEmail, "ix_users_normalized_email")
                .IsUnique();

            // Substring search (ILIKE '%text%')
            b.HasIndex(e => e.NormalizedUserName, "ix_users_normalized_user_name_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");
            b.HasIndex(e => e.NormalizedEmail, "ix_users_normalized_email_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            b.HasMany(e => e.UserRoles).WithOne(e => e.User).HasForeignKey(e => e.UserId);
            b.HasMany(e => e.TakenUsernames).WithOne(e => e.User).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<RoleEntity>(b =>
        {
            b.ToTable("roles", t =>
                t.HasCheckConstraint("ck_roles_name_lowercase", "name = lower(name)"));
            b.HasKey(e => e.Id);

            b.Property(e => e.Name).HasMaxLength(RoleValidator.MaxNameLength);
            b.Property(e => e.Description).HasMaxLength(RoleValidator.MaxDescriptionLength);

            b.HasIndex(e => e.Name, "ix_roles_name").IsUnique();

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
            b.HasOne(e => e.User).WithMany(e => e.UserRoles).HasForeignKey(e => e.UserId);
            b.HasOne(e => e.Role).WithMany(e => e.UserRoles).HasForeignKey(e => e.RoleId);
            b.HasOne(e => e.Tenant).WithMany(e => e.UserRoles).HasForeignKey(e => e.TenantId);
        });

        modelBuilder.Entity<TakenUsernameEntity>(b =>
        {
            b.ToTable("taken_usernames");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.NormalizedUsername).IsUnique();
            b.HasOne(e => e.User).WithMany(e => e.TakenUsernames).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<ClientEntity>(b =>
        {
            b.ToTable("clients");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.ClientId).IsUnique();
            b.HasOne(e => e.Tenant).WithMany(t => t.Clients).HasForeignKey(e => e.TenantId);
        });

        modelBuilder.Entity<ScopeEntity>(b =>
        {
            b.ToTable("scopes", t =>
                t.HasCheckConstraint("ck_scopes_name_lowercase", "name = lower(name)"));
            b.HasKey(e => e.Id);

            b.Property(e => e.Name).HasMaxLength(ScopeValidator.MaxNameLength);
            b.Property(e => e.DisplayName).HasMaxLength(ScopeValidator.MaxDisplayNameLength);

            b.HasIndex(e => e.Name, "ix_scopes_name").IsUnique();

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
            b.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId);
            b.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<RefreshTokenEntity>(b =>
        {
            b.ToTable("refresh_tokens");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.TokenHash).IsUnique();
            b.HasIndex(e => e.FamilyId);
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

        modelBuilder.Entity<UserTokenEntity>(b =>
        {
            b.ToTable("user_tokens");
            b.HasIndex(e => e.TokenHash).IsUnique();
            b.HasIndex(e => e.UserId);
            b.HasIndex(e => e.ExpiresAt);
        });

        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}

