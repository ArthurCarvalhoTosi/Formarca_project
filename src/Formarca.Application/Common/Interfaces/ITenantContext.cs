namespace Formarca.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid? TenantId { get; }

    bool IsPlatformAdmin { get; }

    bool IgnoreTenantFilters { get; }

    void SetTenant(Guid? tenantId, bool isPlatformAdmin = false);

    void SetIgnoreTenantFilters(bool ignore);
}
