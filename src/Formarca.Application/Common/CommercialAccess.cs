using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;

namespace Formarca.Application.Common;

internal static class CommercialAccess
{
    public static void IgnoreTenantFiltersIfAdmin(ITenantContext tenantContext, ICurrentUser currentUser)
    {
        if (currentUser.IsPlatformAdmin)
        {
            tenantContext.SetIgnoreTenantFilters(true);
        }
    }

    public static Guid ResolveTenantId(ICurrentUser currentUser, Guid? requestedTenantId, bool requiredForAdmin)
    {
        if (currentUser.IsPlatformAdmin)
        {
            if (requiredForAdmin && (requestedTenantId is null || requestedTenantId == Guid.Empty))
            {
                throw new ConflictException("TenantId is required.");
            }

            return requestedTenantId ?? Guid.Empty;
        }

        if (currentUser.TenantId is null)
        {
            throw new ForbiddenException("No active tenant.");
        }

        if (requestedTenantId is not null
            && requestedTenantId != Guid.Empty
            && requestedTenantId != currentUser.TenantId)
        {
            throw new ForbiddenException("User is not a member of the requested tenant.");
        }

        return currentUser.TenantId.Value;
    }

    public static void EnsureRequestedTenantsAllowed(
        ICurrentUser currentUser,
        IReadOnlyList<Guid>? requestedTenantIds)
    {
        if (requestedTenantIds is not { Count: > 0 })
        {
            return;
        }

        foreach (var tenantId in requestedTenantIds)
        {
            ResolveTenantId(currentUser, tenantId, requiredForAdmin: false);
        }
    }
}
