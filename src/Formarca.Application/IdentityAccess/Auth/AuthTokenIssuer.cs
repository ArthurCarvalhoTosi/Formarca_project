using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.Common;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.IdentityAccess.Auth;

public static class AuthTokenIssuer
{
    public static async Task<(AuthTokensDto Tokens, IReadOnlyList<string> Permissions)> IssueAsync(
        IGenericRepository repository,
        IJwtTokenService jwt,
        Guid userId,
        string email,
        bool isPlatformAdmin,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var permissions = await ResolvePermissionsAsync(repository, userId, tenantId, isPlatformAdmin, cancellationToken);
        var accessToken = jwt.CreateAccessToken(userId, email, tenantId, isPlatformAdmin, permissions);
        var refresh = jwt.CreateRefreshToken();

        repository.Add(RefreshToken.Create(userId, refresh.TokenHash, refresh.ExpiresAtUtc, tenantId));
        await repository.CommitAsync(cancellationToken);

        var tokens = new AuthTokensDto(
            accessToken,
            refresh.RawToken,
            Clock.Current.UtcNow.AddMinutes(30),
            tenantId);

        return (tokens, permissions);
    }

    public static async Task<IReadOnlyList<TenantSummaryDto>> GetUserTenantsAsync(
        IGenericRepository repository,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await (
            from m in repository.Query<UserTenantMembership>()
            join t in repository.Query<Tenant>() on m.TenantId equals t.Id
            where m.UserId == userId && m.IsActive && !m.IsDeleted && !t.IsDeleted
            orderby m.IsDefault descending, t.Name
            select new TenantSummaryDto(t.Id, t.Name, t.Slug, m.IsDefault)
        ).ToListAsync(cancellationToken);
    }

    public static async Task<IReadOnlyList<string>> ResolvePermissionsAsync(
        IGenericRepository repository,
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

        var groupIds = await repository.Query<UserGroupMembership>()
            .Where(x => x.UserId == userId && x.TenantId == tenantId && !x.IsDeleted)
            .Select(x => x.GroupId)
            .ToListAsync(cancellationToken);

        if (groupIds.Count == 0)
        {
            return [];
        }

        return await (
            from gp in repository.Query<GroupPermission>()
            join p in repository.Query<Permission>() on gp.PermissionId equals p.Id
            where groupIds.Contains(gp.GroupId) && !gp.IsDeleted && !p.IsDeleted
            select p.Code
        ).Distinct().ToListAsync(cancellationToken);
    }

    public static async Task EnsureDefaultTenantGroupsAsync(
        IGenericRepository repository,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var existing = await repository.Query<Group>()
            .Where(g => g.TenantId == tenantId && !g.IsDeleted)
            .Select(g => g.Name)
            .ToListAsync(cancellationToken);

        var permissions = await repository.Query<Permission>()
            .Where(p => !p.IsDeleted)
            .ToDictionaryAsync(p => p.Code, p => p.Id, cancellationToken);

        if (!existing.Contains(DefaultGroupNames.TenantAdmins))
        {
            var admins = Group.Create(DefaultGroupNames.TenantAdmins, tenantId, "Tenant administrators");
            repository.Add(admins);
            await repository.CommitAsync(cancellationToken);

            foreach (var code in new[]
                     {
                         PermissionCodes.UsersRead, PermissionCodes.UsersManage,
                         PermissionCodes.GroupsRead, PermissionCodes.GroupsManage,
                         PermissionCodes.ProcessesRead
                     })
            {
                if (permissions.TryGetValue(code, out var permissionId))
                {
                    repository.Add(GroupPermission.Create(admins.Id, permissionId));
                }
            }

            await repository.CommitAsync(cancellationToken);
        }

        if (!existing.Contains(DefaultGroupNames.ClientUsers))
        {
            var clients = Group.Create(DefaultGroupNames.ClientUsers, tenantId, "Client users");
            repository.Add(clients);
            await repository.CommitAsync(cancellationToken);

            if (permissions.TryGetValue(PermissionCodes.ProcessesRead, out var permissionId))
            {
                repository.Add(GroupPermission.Create(clients.Id, permissionId));
                await repository.CommitAsync(cancellationToken);
            }
        }
    }
}
