using Formarca.Domain.Common;

namespace Formarca.Domain.IdentityAccess;

/// <summary>
/// Permission group. Null <see cref="TenantId"/> means a platform-level group.
/// </summary>
public class Group : Entity
{
    private readonly List<GroupPermission> _permissions = [];

    private Group()
    {
    }

    public Guid? TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public IReadOnlyCollection<GroupPermission> Permissions => _permissions.AsReadOnly();

    public static Group Create(string name, Guid? tenantId = null, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Group
        {
            Name = name.Trim(),
            TenantId = tenantId,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };
    }

    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        MarkUpdated();
    }

    public void AddPermission(Guid permissionId)
    {
        if (_permissions.Any(p => p.PermissionId == permissionId))
        {
            return;
        }

        _permissions.Add(GroupPermission.Create(Id, permissionId));
        MarkUpdated();
    }

    public void RemovePermission(Guid permissionId)
    {
        var existing = _permissions.FirstOrDefault(p => p.PermissionId == permissionId);
        if (existing is null)
        {
            return;
        }

        _permissions.Remove(existing);
        MarkUpdated();
    }

    public void SetPermissions(IEnumerable<Guid> permissionIds)
    {
        _permissions.Clear();
        foreach (var permissionId in permissionIds.Distinct())
        {
            _permissions.Add(GroupPermission.Create(Id, permissionId));
        }

        MarkUpdated();
    }
}
