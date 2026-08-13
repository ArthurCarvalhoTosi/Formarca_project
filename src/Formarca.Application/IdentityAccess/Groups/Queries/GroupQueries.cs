using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.IdentityAccess.Groups.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.IdentityAccess.Groups.Queries;

public sealed record ListGroupsQuery(Guid? TenantId, int Page = 1, int PageSize = Paging.DefaultPageSize)
    : IRequest<PagedResult<GroupDto>>;

public sealed class ListGroupsQueryHandler(
    IApplicationDbContext db,
    ITenantContext tenantContext) : IRequestHandler<ListGroupsQuery, PagedResult<GroupDto>>
{
    public async Task<PagedResult<GroupDto>> Handle(ListGroupsQuery request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var (page, pageSize, skip) = Paging.Normalize(request.Page, request.PageSize);

        var query = db.Groups.AsNoTracking()
            .Where(g => request.TenantId == null || g.TenantId == request.TenantId || g.TenantId == null)
            .OrderBy(g => g.Name);

        var total = await query.CountAsync(cancellationToken);
        var groups = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        var items = new List<GroupDto>(groups.Count);
        foreach (var group in groups)
        {
            items.Add(await CreateGroupCommandHandler.MapGroupAsync(db, group.Id, cancellationToken));
        }

        return new PagedResult<GroupDto>(items, total, page, pageSize);
    }
}

public sealed record GetGroupByIdQuery(Guid Id) : IRequest<GroupDto>;

public sealed class GetGroupByIdQueryHandler(
    IApplicationDbContext db,
    ITenantContext tenantContext) : IRequestHandler<GetGroupByIdQuery, GroupDto>
{
    public async Task<GroupDto> Handle(GetGroupByIdQuery request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var exists = await db.Groups.AsNoTracking().AnyAsync(g => g.Id == request.Id, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("Group not found.");
        }

        return await CreateGroupCommandHandler.MapGroupAsync(db, request.Id, cancellationToken);
    }
}

public sealed record ListPermissionsQuery : IRequest<IReadOnlyList<PermissionDto>>;

public sealed class ListPermissionsQueryHandler(
    IApplicationDbContext db,
    ITenantContext tenantContext) : IRequestHandler<ListPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    public async Task<IReadOnlyList<PermissionDto>> Handle(ListPermissionsQuery request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        return await db.Permissions.AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new PermissionDto(p.Id, p.Code, p.Name))
            .ToListAsync(cancellationToken);
    }
}
