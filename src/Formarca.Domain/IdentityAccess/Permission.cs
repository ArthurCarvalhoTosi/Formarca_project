using Formarca.Domain.Common;
using Formarca.Domain.Tenancy;

namespace Formarca.Domain.IdentityAccess;

public class Permission : Entity, IPlatformEntity
{
    private Permission()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public static Permission Create(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Permission
        {
            Code = code.Trim().ToLowerInvariant(),
            Name = name.Trim()
        };
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        MarkUpdated();
    }
}
