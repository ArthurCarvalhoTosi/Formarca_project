namespace Formarca.Domain.Tenancy;

public enum TenantStatus
{
    Active = 0,
    Suspended = 1,
    Inactive = 2,
    Pending = 3,
    Onboarding = 4
}

public static class TenantStatusExtensions
{
    public static bool AllowsClientAccess(this TenantStatus status) =>
        status is TenantStatus.Active or TenantStatus.Onboarding;
}
