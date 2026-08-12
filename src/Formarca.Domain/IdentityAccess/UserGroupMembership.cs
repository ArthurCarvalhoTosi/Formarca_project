using Formarca.Domain.Common;
using Formarca.Domain.Tenancy;

namespace Formarca.Domain.IdentityAccess;

public class UserGroupMembership : Entity, ITenantScoped
{
    private UserGroupMembership()
    {
    }

    public Guid UserId { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid GroupId { get; private set; }

    public static UserGroupMembership Create(Guid userId, Guid tenantId, Guid groupId)
    {
        return new UserGroupMembership
        {
            UserId = userId,
            TenantId = tenantId,
            GroupId = groupId
        };
    }
}
