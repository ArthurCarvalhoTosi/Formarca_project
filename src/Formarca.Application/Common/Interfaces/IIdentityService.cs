namespace Formarca.Application.Common.Interfaces;

public sealed record IdentityUserInfo(
    Guid Id,
    string Email,
    string? DisplayName,
    bool IsPlatformAdmin,
    bool EmailConfirmed,
    bool IsActive);

public interface IIdentityService
{
    Task<IdentityUserInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IdentityUserInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, Guid? UserId, IReadOnlyList<string> Errors)> CreateUserAsync(
        string email,
        string password,
        string? displayName,
        bool isPlatformAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IReadOnlyList<string> Errors)> UpdateUserAsync(
        Guid userId,
        string? displayName,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IdentityUserInfo>> ListUsersAsync(CancellationToken cancellationToken = default);
}
