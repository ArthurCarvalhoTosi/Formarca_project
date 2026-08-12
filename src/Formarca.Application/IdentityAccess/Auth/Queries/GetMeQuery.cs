using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using MediatR;

namespace Formarca.Application.IdentityAccess.Auth.Queries;

public sealed record GetMeQuery : IRequest<CurrentUserDto>;

public sealed class GetMeQueryHandler(
    ICurrentUser currentUser,
    IIdentityService identity,
    IApplicationDbContext db,
    ITenantContext tenantContext) : IRequestHandler<GetMeQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new UnauthorizedAppException("Not authenticated.");
        }

        tenantContext.SetIgnoreTenantFilters(true);

        var user = await identity.FindByIdAsync(currentUser.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var tenants = await AuthTokenIssuer.GetUserTenantsAsync(db, user.Id, cancellationToken);
        var permissions = await AuthTokenIssuer.ResolvePermissionsAsync(
            db, user.Id, currentUser.TenantId, user.IsPlatformAdmin, cancellationToken);

        return new CurrentUserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsPlatformAdmin,
            currentUser.TenantId,
            tenants,
            permissions);
    }
}
