using Formarca.Application.Common.Interfaces;
using Formarca.Domain.Common;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.PlatformContent;
using Formarca.Domain.Tenancy;
using Formarca.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantContext tenantContext)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IApplicationDbContext
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();

    public DbSet<UserTenantMembership> UserTenantMemberships => Set<UserTenantMembership>();

    public DbSet<UserGroupMembership> UserGroupMemberships => Set<UserGroupMembership>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PlatformNotice> PlatformNotices => Set<PlatformNotice>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Keep default AspNet* table names from Identity.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        builder.Entity<UserTenantMembership>()
            .HasQueryFilter(e =>
                !e.IsDeleted &&
                (tenantContext.IgnoreTenantFilters
                 || (tenantContext.TenantId != null && e.TenantId == tenantContext.TenantId.Value)));

        builder.Entity<UserGroupMembership>()
            .HasQueryFilter(e =>
                !e.IsDeleted &&
                (tenantContext.IgnoreTenantFilters
                 || (tenantContext.TenantId != null && e.TenantId == tenantContext.TenantId.Value)));
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.MarkUpdated();
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
