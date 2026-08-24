using System.Reflection;
using System.Text.Json;
using Formarca.Application.Common.Interfaces;
using Formarca.Domain.Auditing;
using Formarca.Domain.Brands;
using Formarca.Domain.Common;
using Formarca.Domain.Contracts;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Invoices;
using Formarca.Domain.PlatformContent;
using Formarca.Domain.Tenancy;
using Formarca.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ProcessEntity = Formarca.Domain.Processes.Process;

namespace Formarca.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private static readonly MethodInfo SoftDeleteFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo TenantAndSoftDeleteFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(SetTenantAndSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ITenantContext _tenantContext;
    private readonly IClock _clock;
    private readonly ICurrentUser? _currentUser;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantContext tenantContext,
        IClock clock,
        ICurrentUser? currentUser = null)
        : base(options)
    {
        _tenantContext = tenantContext;
        _clock = clock;
        _currentUser = currentUser;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();

    public DbSet<UserTenantMembership> UserTenantMemberships => Set<UserTenantMembership>();

    public DbSet<UserGroupMembership> UserGroupMemberships => Set<UserGroupMembership>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PlatformNotice> PlatformNotices => Set<PlatformNotice>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<InvoiceSettings> InvoiceSettings => Set<InvoiceSettings>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<ProcessEntity> Processes => Set<ProcessEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplyQueryFilters(builder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareSave();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepareSave();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareSave()
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.MarkUpdated();
            }
        }

        var auditLogs = BuildAuditLogs();
        if (auditLogs.Count > 0)
        {
            AuditLogs.AddRange(auditLogs);
        }
    }

    private void ApplyQueryFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            if (clr.IsAbstract || !typeof(Entity).IsAssignableFrom(clr))
            {
                continue;
            }

            if (typeof(ITenantScoped).IsAssignableFrom(clr))
            {
                TenantAndSoftDeleteFilterMethod.MakeGenericMethod(clr)
                    .Invoke(this, [builder]);
            }
            else
            {
                SoftDeleteFilterMethod.MakeGenericMethod(clr)
                    .Invoke(this, [builder]);
            }
        }
    }

    // EF evaluates these DbContext members per instance at query time.
    // Capturing ITenantContext in a static filter would pin the first scope forever.
    private bool IgnoreSoftDeleteFilters => _tenantContext.IgnoreSoftDeleteFilters;

    private bool IgnoreTenantFilters => _tenantContext.IgnoreTenantFilters;

    private Guid? CurrentTenantId => _tenantContext.TenantId;

    private void SetSoftDeleteFilter<TEntity>(ModelBuilder builder)
        where TEntity : Entity
    {
        builder.Entity<TEntity>().HasQueryFilter(e =>
            IgnoreSoftDeleteFilters || !e.IsDeleted);
    }

    private void SetTenantAndSoftDeleteFilter<TEntity>(ModelBuilder builder)
        where TEntity : Entity, ITenantScoped
    {
        builder.Entity<TEntity>().HasQueryFilter(e =>
            (IgnoreSoftDeleteFilters || !e.IsDeleted) &&
            (IgnoreTenantFilters || e.TenantId == CurrentTenantId));
    }

    private List<AuditLog> BuildAuditLogs()
    {
        var logs = new List<AuditLog>();
        var now = _clock.UtcNow;
        var userId = _currentUser?.UserId;
        var tenantId = _currentUser?.TenantId ?? _tenantContext.TenantId;

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            var action = entry.State switch
            {
                EntityState.Added => "Added",
                EntityState.Deleted => "Deleted",
                EntityState.Modified when entry.Entity.IsDeleted => "Deleted",
                _ => "Modified"
            };

            logs.Add(new AuditLog
            {
                TenantId = tenantId,
                UserId = userId,
                Action = action,
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = entry.Entity.Id,
                OccurredAtUtc = now,
                JsonChanges = SerializeChanges(entry)
            });
        }

        return logs;
    }

    private static string? SerializeChanges(EntityEntry<Entity> entry)
    {
        var changes = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.Name is nameof(Entity.DomainEvents))
            {
                continue;
            }

            var include = entry.State == EntityState.Added
                || entry.State == EntityState.Deleted
                || property.IsModified;

            if (!include)
            {
                continue;
            }

            changes[property.Metadata.Name] = new
            {
                old = entry.State == EntityState.Added ? null : property.OriginalValue,
                @new = entry.State == EntityState.Deleted ? null : property.CurrentValue
            };
        }

        return changes.Count == 0 ? null : JsonSerializer.Serialize(changes, JsonOptions);
    }
}
