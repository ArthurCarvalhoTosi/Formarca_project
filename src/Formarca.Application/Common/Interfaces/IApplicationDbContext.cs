using Formarca.Domain.IdentityAccess;
using Formarca.Domain.PlatformContent;
using Formarca.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }

    DbSet<Permission> Permissions { get; }

    DbSet<Group> Groups { get; }

    DbSet<GroupPermission> GroupPermissions { get; }

    DbSet<UserTenantMembership> UserTenantMemberships { get; }

    DbSet<UserGroupMembership> UserGroupMemberships { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<PlatformNotice> PlatformNotices { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
