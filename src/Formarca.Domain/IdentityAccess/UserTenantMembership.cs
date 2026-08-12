using Formarca.Domain.Common;
using Formarca.Domain.Tenancy;

namespace Formarca.Domain.IdentityAccess;

public class UserTenantMembership : Entity, ITenantScoped
{
    private UserTenantMembership()
    {
    }

    public Guid UserId { get; private set; }

    public Guid TenantId { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static UserTenantMembership Create(Guid userId, Guid tenantId, bool isDefault = false)
    {
        return new UserTenantMembership
        {
            UserId = userId,
            TenantId = tenantId,
            IsDefault = isDefault,
            IsActive = true
        };
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }
}
