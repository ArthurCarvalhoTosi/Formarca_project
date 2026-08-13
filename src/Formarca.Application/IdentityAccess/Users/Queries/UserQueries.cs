using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.IdentityAccess.Auth;
using MediatR;

namespace Formarca.Application.IdentityAccess.Users.Queries;

public sealed record ListUsersQuery(int Page = 1, int PageSize = Paging.DefaultPageSize)
    : IRequest<PagedResult<UserDto>>;

public sealed class ListUsersQueryHandler(
    IIdentityService identity,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<ListUsersQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var (page, pageSize, skip) = Paging.Normalize(request.Page, request.PageSize);
        var (users, total) = await identity.ListUsersAsync(skip, pageSize, cancellationToken);
        var result = new List<UserDto>(users.Count);

        foreach (var user in users)
        {
            var tenants = await AuthTokenIssuer.GetUserTenantsAsync(repository, user.Id, cancellationToken);
            result.Add(new UserDto(user.Id, user.Email, user.DisplayName, user.IsPlatformAdmin, user.IsActive, tenants));
        }

        return new PagedResult<UserDto>(result, total, page, pageSize);
    }
}

public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;

public sealed class GetUserByIdQueryHandler(
    IIdentityService identity,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var user = await identity.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var tenants = await AuthTokenIssuer.GetUserTenantsAsync(repository, user.Id, cancellationToken);
        return new UserDto(user.Id, user.Email, user.DisplayName, user.IsPlatformAdmin, user.IsActive, tenants);
    }
}
