namespace Formarca.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Email { get; }

    Guid? TenantId { get; }

    bool IsPlatformAdmin { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Permissions { get; }
}
