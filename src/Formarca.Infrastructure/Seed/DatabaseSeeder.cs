using Formarca.Domain.Common;
using Formarca.Domain.IdentityAccess;
using Formarca.Infrastructure.Identity;
using Formarca.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Formarca.Infrastructure.Seed;

public static class DatabaseSeeder
{
    public const string PlatformAdminEmail = "admin@formarca.com";
    public const string PlatformAdminPassword = "Formarca@Admin123!";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
        var db = sp.GetRequiredService<AppDbContext>();
        var repository = sp.GetRequiredService<Application.Common.Interfaces.IGenericRepository>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var tenantContext = sp.GetRequiredService<Application.Common.Interfaces.ITenantContext>();

        tenantContext.SetIgnoreTenantFilters(true);

        await SeedPermissionsAsync(db, cancellationToken);
        await SeedPlatformAdminAsync(userManager, logger, cancellationToken);
        await SeedDemoTenantAsync(db, repository, userManager, cancellationToken);
    }

    private static async Task SeedPermissionsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var existing = await db.Permissions.Select(p => p.Code).ToListAsync(cancellationToken);
        foreach (var (code, name) in PermissionCodes.Catalog)
        {
            if (existing.Contains(code))
            {
                continue;
            }

            db.Permissions.Add(Permission.Create(code, name));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedPlatformAdminAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var admin = await userManager.FindByEmailAsync(PlatformAdminEmail);
        if (admin is not null)
        {
            return;
        }

        admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = PlatformAdminEmail,
            Email = PlatformAdminEmail,
            EmailConfirmed = true,
            DisplayName = "Formarca Platform Admin",
            IsPlatformAdmin = true,
            IsActive = true,
            CreatedAtUtc = Clock.Current.UtcNow
        };

        var result = await userManager.CreateAsync(admin, PlatformAdminPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed platform admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        logger.LogInformation("Seeded platform admin {Email}", PlatformAdminEmail);
    }

    private static async Task SeedDemoTenantAsync(
        AppDbContext db,
        Application.Common.Interfaces.IGenericRepository repository,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken)
    {
        const string slug = "demo";
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug && !t.IsDeleted, cancellationToken);
        if (tenant is null)
        {
            tenant = Domain.Tenancy.Tenant.Create("Demo Tenant", slug, "00.000.000/0000-00");
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(cancellationToken);
        }

        await Application.IdentityAccess.Auth.AuthTokenIssuer.EnsureDefaultTenantGroupsAsync(repository, tenant.Id, cancellationToken);

        var admin = await userManager.FindByEmailAsync(PlatformAdminEmail);
        if (admin is null)
        {
            return;
        }

        var membershipExists = await db.UserTenantMemberships
            .IgnoreQueryFilters()
            .AnyAsync(m => m.UserId == admin.Id && m.TenantId == tenant.Id && !m.IsDeleted, cancellationToken);

        if (!membershipExists)
        {
            db.UserTenantMemberships.Add(UserTenantMembership.Create(admin.Id, tenant.Id, isDefault: true));
            await db.SaveChangesAsync(cancellationToken);
        }

        var adminGroup = await db.Groups.FirstOrDefaultAsync(
            g => g.TenantId == tenant.Id && g.Name == DefaultGroupNames.TenantAdmins && !g.IsDeleted,
            cancellationToken);

        if (adminGroup is not null)
        {
            var inGroup = await db.UserGroupMemberships
                .IgnoreQueryFilters()
                .AnyAsync(
                    g => g.UserId == admin.Id && g.GroupId == adminGroup.Id && !g.IsDeleted,
                    cancellationToken);

            if (!inGroup)
            {
                db.UserGroupMemberships.Add(UserGroupMembership.Create(admin.Id, tenant.Id, adminGroup.Id));
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
