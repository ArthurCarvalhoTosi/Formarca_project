using Formarca.Application.Common.Interfaces;
using Formarca.Domain.IdentityAccess;

namespace Formarca.Web.Middleware;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ICurrentUser currentUser)
    {
        Guid? tenantId = currentUser.TenantId;

        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValues)
            && Guid.TryParse(headerValues.FirstOrDefault(), out var headerTenantId))
        {
            tenantId = headerTenantId;
        }

        tenantContext.SetTenant(tenantId, currentUser.IsPlatformAdmin);

        // Admin routes may need cross-tenant visibility; controllers/handlers set IgnoreTenantFilters explicitly.
        if (currentUser.IsPlatformAdmin
            && context.Request.Path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase))
        {
            tenantContext.SetIgnoreTenantFilters(true);
        }

        await next(context);
    }
}
