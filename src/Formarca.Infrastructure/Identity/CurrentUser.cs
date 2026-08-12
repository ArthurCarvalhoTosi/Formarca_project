using System.Security.Claims;
using Formarca.Application.Common.Interfaces;
using Formarca.Domain.IdentityAccess;
using Microsoft.AspNetCore.Http;

namespace Formarca.Infrastructure.Identity;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email =>
        Principal?.FindFirstValue(ClaimTypes.Email)
        ?? Principal?.FindFirstValue("email");

    public Guid? TenantId
    {
        get
        {
            var value = Principal?.FindFirstValue("tenant_id");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsPlatformAdmin =>
        Principal?.FindFirstValue(PlatformRoles.ClaimType) == PlatformRoles.PlatformAdmin;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public IReadOnlyCollection<string> Permissions =>
        Principal?.FindAll(PermissionCodes.ClaimType).Select(c => c.Value).Distinct().ToArray()
        ?? [];
}
