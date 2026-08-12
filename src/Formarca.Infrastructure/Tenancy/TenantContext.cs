using Formarca.Application.Common.Interfaces;

namespace Formarca.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public bool IsPlatformAdmin { get; private set; }

    public bool IgnoreTenantFilters { get; private set; }

    public void SetTenant(Guid? tenantId, bool isPlatformAdmin = false)
    {
        TenantId = tenantId;
        IsPlatformAdmin = isPlatformAdmin;
    }

    public void SetIgnoreTenantFilters(bool ignore) => IgnoreTenantFilters = ignore;
}
