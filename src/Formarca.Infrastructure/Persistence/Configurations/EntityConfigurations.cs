using Formarca.Domain.Auditing;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.PlatformContent;
using Formarca.Domain.Tenancy;
using Formarca.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Formarca.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Document).HasMaxLength(32);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => new { x.TenantId, x.Name });
        builder.Ignore(x => x.DomainEvents);

        builder.HasMany(x => x.Permissions)
            .WithOne()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Permissions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class GroupPermissionConfiguration : IEntityTypeConfiguration<GroupPermission>
{
    public void Configure(EntityTypeBuilder<GroupPermission> builder)
    {
        builder.ToTable("GroupPermissions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GroupId, x.PermissionId }).IsUnique();
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class UserTenantMembershipConfiguration : IEntityTypeConfiguration<UserTenantMembership>
{
    public void Configure(EntityTypeBuilder<UserTenantMembership> builder)
    {
        builder.ToTable("UserTenantMemberships");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.TenantId }).IsUnique();
        builder.HasIndex(x => x.TenantId);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class UserGroupMembershipConfiguration : IEntityTypeConfiguration<UserGroupMembership>
{
    public void Configure(EntityTypeBuilder<UserGroupMembership> builder)
    {
        builder.ToTable("UserGroupMemberships");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.TenantId, x.GroupId }).IsUnique();
        builder.HasIndex(x => x.TenantId);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class PlatformNoticeConfiguration : IEntityTypeConfiguration<PlatformNotice>
{
    public void Configure(EntityTypeBuilder<PlatformNotice> builder)
    {
        builder.ToTable("PlatformNotices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(32).IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => new { x.EntityName, x.EntityId });
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.UserId);
    }
}

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.DisplayName).HasMaxLength(200);
    }
}
