using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.IdentityAccess.Auth;

public static class AuthTokenIssuer
{
    public static async Task<(AuthTokensDto Tokens, IReadOnlyList<string> Permissions)> IssueAsync(
        IApplicationDbContext db,
        IJwtTokenService jwt,
        Guid userId,
        string email,
        bool isPlatformAdmin,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var permissions = await ResolvePermissionsAsync(db, userId, tenantId, isPlatformAdmin, cancellationToken);
        var accessToken = jwt.CreateAccessToken(userId, email, tenantId, isPlatformAdmin, permissions);
        var refresh = jwt.CreateRefreshToken();

        db.RefreshTokens.Add(RefreshToken.Create(userId, refresh.TokenHash, refresh.ExpiresAtUtc, tenantId));
        await db.SaveChangesAsync(cancellationToken);

        var tokens = new AuthTokensDto(
            accessToken,
            refresh.RawToken,
            DateTime.UtcNow.AddMinutes(30),
            tenantId);

        return (tokens, permissions);
    }

    public static async Task<IReadOnlyList<TenantSummaryDto>> GetUserTenantsAsync(
        IApplicationDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await (
            from m in db.UserTenantMemberships.AsNoTracking()
            join t in db.Tenants.AsNoTracking() on m.TenantId equals t.Id
            where m.UserId == userId && m.IsActive && !m.IsDeleted && !t.IsDeleted
            orderby m.IsDefault descending, t.Name
            select new TenantSummaryDto(t.Id, t.Name, t.Slug, m.IsDefault)
        ).ToListAsync(cancellationToken);
    }

    public static async Task<IReadOnlyList<string>> ResolvePermissionsAsync(
        IApplicationDbContext db,
        Guid userId,
        Guid? tenantId,
        bool isPlatformAdmin,
        CancellationToken cancellationToken)
    {
        if (isPlatformAdmin)
        {
            return PermissionCodes.Catalog.Select(c => c.Code).ToList();
        }

        if (tenantId is null)
        {
            return [];
        }

        var groupIds = await db.UserGroupMemberships.AsNoTracking()
            .Where(x => x.UserId == userId && x.TenantId == tenantId && !x.IsDeleted)
            .Select(x => x.GroupId)
            .ToListAsync(cancellationToken);

        if (groupIds.Count == 0)
        {
            return [];
        }

        return await (
            from gp in db.GroupPermissions.AsNoTracking()
            join p in db.Permissions.AsNoTracking() on gp.PermissionId equals p.Id
            where groupIds.Contains(gp.GroupId) && !gp.IsDeleted && !p.IsDeleted
            select p.Code
        ).Distinct().ToListAsync(cancellationToken);
    }

    public static async Task EnsureDefaultTenantGroupsAsync(
        IApplicationDbContext db,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var existing = await db.Groups
            .Where(g => g.TenantId == tenantId && !g.IsDeleted)
            .Select(g => g.Name)
            .ToListAsync(cancellationToken);

        var permissions = await db.Permissions.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .ToDictionaryAsync(p => p.Code, p => p.Id, cancellationToken);

        if (!existing.Contains(DefaultGroupNames.TenantAdmins))
        {
            var admins = Group.Create(DefaultGroupNames.TenantAdmins, tenantId, "Tenant administrators");
            db.Groups.Add(admins);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var code in new[]
                     {
                         PermissionCodes.UsersRead, PermissionCodes.UsersManage,
                         PermissionCodes.GroupsRead, PermissionCodes.GroupsManage,
                         PermissionCodes.ProcessesRead
                     })
            {
                if (permissions.TryGetValue(code, out var permissionId))
                {
                    db.GroupPermissions.Add(GroupPermission.Create(admins.Id, permissionId));
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        if (!existing.Contains(DefaultGroupNames.ClientUsers))
        {
            var clients = Group.Create(DefaultGroupNames.ClientUsers, tenantId, "Client users");
            db.Groups.Add(clients);
            await db.SaveChangesAsync(cancellationToken);

            if (permissions.TryGetValue(PermissionCodes.ProcessesRead, out var permissionId))
            {
                db.GroupPermissions.Add(GroupPermission.Create(clients.Id, permissionId));
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
