using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.IdentityAccess.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.IdentityAccess.Users.Queries;

public sealed record ListUsersQuery : IRequest<IReadOnlyList<UserDto>>;

public sealed class ListUsersQueryHandler(
    IIdentityService identity,
    IApplicationDbContext db,
    ITenantContext tenantContext) : IRequestHandler<ListUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var users = await identity.ListUsersAsync(cancellationToken);
        var result = new List<UserDto>(users.Count);

        foreach (var user in users)
        {
            var tenants = await AuthTokenIssuer.GetUserTenantsAsync(db, user.Id, cancellationToken);
            result.Add(new UserDto(user.Id, user.Email, user.DisplayName, user.IsPlatformAdmin, user.IsActive, tenants));
        }

        return result;
    }
}

public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;

public sealed class GetUserByIdQueryHandler(
    IIdentityService identity,
    IApplicationDbContext db,
    ITenantContext tenantContext) : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var user = await identity.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var tenants = await AuthTokenIssuer.GetUserTenantsAsync(db, user.Id, cancellationToken);
        return new UserDto(user.Id, user.Email, user.DisplayName, user.IsPlatformAdmin, user.IsActive, tenants);
    }
}
