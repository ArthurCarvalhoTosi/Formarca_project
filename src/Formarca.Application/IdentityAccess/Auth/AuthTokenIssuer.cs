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
        CancellationToken cancellationToken,
        bool includeNonActiveTenants = false)
    {
        var query =
            from m in repository.Query<UserTenantMembership>()
            join t in repository.Query<Tenant>() on m.TenantId equals t.Id
            where m.UserId == userId && m.IsActive && !m.IsDeleted && !t.IsDeleted
            select new { m, t };

        if (!includeNonActiveTenants)
        {
            query = query.Where(x =>
                x.t.Status == TenantStatus.Active || x.t.Status == TenantStatus.Onboarding);
        }

        return await query
            .OrderByDescending(x => x.m.IsDefault)
            .ThenBy(x => x.t.Name)
            .Select(x => new TenantSummaryDto(x.t.Id, x.t.Name, x.t.Slug, x.m.IsDefault, x.t.Status.ToString()))
            .ToListAsync(cancellationToken);
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

    private static readonly string[] TenantAdminPermissionCodes =
    [
        PermissionCodes.UsersRead, PermissionCodes.UsersManage,
        PermissionCodes.GroupsRead, PermissionCodes.GroupsManage,
        PermissionCodes.ProcessesRead,
        PermissionCodes.RegistrationsRead, PermissionCodes.RegistrationsManage,
        PermissionCodes.BrandsRead,
        PermissionCodes.ContractsRead, PermissionCodes.ContractsSign,
        PermissionCodes.InvoicesRead, PermissionCodes.InvoicesPay
    ];

    private static readonly string[] ClientUserPermissionCodes =
    [
        PermissionCodes.ProcessesRead,
        PermissionCodes.RegistrationsRead, PermissionCodes.RegistrationsManage,
        PermissionCodes.BrandsRead,
        PermissionCodes.ContractsRead,
        PermissionCodes.InvoicesRead
    ];

    private static readonly string[] ClientOnboardingPermissionCodes =
    [
        PermissionCodes.RegistrationsRead, PermissionCodes.RegistrationsManage,
        PermissionCodes.ContractsRead, PermissionCodes.ContractsSign,
        PermissionCodes.InvoicesRead, PermissionCodes.InvoicesPay
    ];

    public static async Task EnsureDefaultTenantGroupsAsync(
        IGenericRepository repository,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var permissions = await repository.Query<Permission>()
            .Where(p => !p.IsDeleted)
            .ToDictionaryAsync(p => p.Code, p => p.Id, cancellationToken);

        await EnsureGroupWithPermissionsAsync(
            repository,
            tenantId,
            DefaultGroupNames.TenantAdmins,
            "Tenant administrators",
            TenantAdminPermissionCodes,
            permissions,
            cancellationToken);

        await EnsureGroupWithPermissionsAsync(
            repository,
            tenantId,
            DefaultGroupNames.ClientUsers,
            "Client users",
            ClientUserPermissionCodes,
            permissions,
            cancellationToken);

        await EnsureGroupWithPermissionsAsync(
            repository,
            tenantId,
            DefaultGroupNames.ClientOnboarding,
            "Client onboarding",
            ClientOnboardingPermissionCodes,
            permissions,
            cancellationToken);
    }

    private static async Task EnsureGroupWithPermissionsAsync(
        IGenericRepository repository,
        Guid tenantId,
        string name,
        string description,
        IReadOnlyList<string> permissionCodes,
        IReadOnlyDictionary<string, Guid> permissions,
        CancellationToken cancellationToken)
    {
        var group = await repository.FirstOrDefaultAsync<Group>(
            g => g.TenantId == tenantId && g.Name == name && !g.IsDeleted, cancellationToken);

        if (group is null)
        {
            group = Group.Create(name, tenantId, description);
            repository.Add(group);
            await repository.CommitAsync(cancellationToken);
        }

        var existingPermissionIds = await repository.Query<GroupPermission>()
            .Where(gp => gp.GroupId == group.Id && !gp.IsDeleted)
            .Select(gp => gp.PermissionId)
            .ToListAsync(cancellationToken);

        var added = false;
        foreach (var code in permissionCodes)
        {
            if (!permissions.TryGetValue(code, out var permissionId)
                || existingPermissionIds.Contains(permissionId))
            {
                continue;
            }

            repository.Add(GroupPermission.Create(group.Id, permissionId));
            added = true;
        }

        if (added)
        {
            await repository.CommitAsync(cancellationToken);
        }
    }
}
