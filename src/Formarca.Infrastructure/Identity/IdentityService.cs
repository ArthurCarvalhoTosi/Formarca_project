using Formarca.Application.Common.Interfaces;
using Formarca.Domain.Common;
using Formarca.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Infrastructure.Identity;

public sealed class IdentityService(UserManager<ApplicationUser> userManager, IClock clock) : IIdentityService
{
    public async Task<IdentityUserInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : Map(user);
    }

    public async Task<IdentityUserInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : Map(user);
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<bool> IsLockedOutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && await userManager.IsLockedOutAsync(user);
    }

    public async Task AccessFailedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return;
        }

        await userManager.AccessFailedAsync(user);
    }

    public async Task ResetAccessFailedCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return;
        }

        await userManager.ResetAccessFailedCountAsync(user);
    }

    public async Task<(bool Succeeded, Guid? UserId, IReadOnlyList<string> Errors)> CreateUserAsync(
        string email,
        string password,
        string? displayName,
        bool isPlatformAdmin,
        CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            IsPlatformAdmin = isPlatformAdmin,
            IsActive = true,
            CreatedAtUtc = clock.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return (false, null, result.Errors.Select(e => e.Description).ToList());
        }

        return (true, user.Id, []);
    }

    public async Task<(bool Succeeded, IReadOnlyList<string> Errors)> UpdateUserAsync(
        Guid userId,
        string? displayName,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return (false, ["User not found."]);
        }

        if (displayName is not null)
        {
            user.DisplayName = displayName;
        }

        if (isActive is not null)
        {
            user.IsActive = isActive.Value;
        }

        var result = await userManager.UpdateAsync(user);
        return result.Succeeded
            ? (true, [])
            : (false, result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<(IReadOnlyList<IdentityUserInfo> Items, int TotalCount)> ListUsersAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = userManager.Users.AsNoTracking().OrderBy(u => u.Email);
        var total = await query.CountAsync(cancellationToken);
        var users = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (users.Select(Map).ToList(), total);
    }

    private static IdentityUserInfo Map(ApplicationUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName, user.IsPlatformAdmin, user.EmailConfirmed, user.IsActive);
}
