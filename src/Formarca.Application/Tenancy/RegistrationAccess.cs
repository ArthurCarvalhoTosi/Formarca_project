using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;

namespace Formarca.Application.Tenancy;

internal static class RegistrationAccess
{
    public static void EnsureCanAccessTenant(ICurrentUser currentUser, Guid tenantId, bool missingSessionIsNotFound)
    {
        if (!currentUser.IsAuthenticated || (!currentUser.IsPlatformAdmin && currentUser.TenantId is null))
        {
            throw missingSessionIsNotFound
                ? new NotFoundException("No active tenant.")
                : new ForbiddenException("No active tenant.");
        }

        if (!currentUser.IsPlatformAdmin && currentUser.TenantId != tenantId)
        {
            throw new ForbiddenException("User is not a member of the requested tenant.");
        }
    }
}
