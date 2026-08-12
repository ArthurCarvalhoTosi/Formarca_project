using Formarca.Domain.Common;

namespace Formarca.Domain.IdentityAccess;

public class GroupPermission : Entity
{
    private GroupPermission()
    {
    }

    public Guid GroupId { get; private set; }

    public Guid PermissionId { get; private set; }

    public static GroupPermission Create(Guid groupId, Guid permissionId)
    {
        return new GroupPermission
        {
            GroupId = groupId,
            PermissionId = permissionId
        };
    }
}
