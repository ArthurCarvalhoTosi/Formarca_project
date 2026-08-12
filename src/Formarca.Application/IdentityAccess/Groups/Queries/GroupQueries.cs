using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.IdentityAccess.Groups.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.IdentityAccess.Groups.Queries;

public sealed record ListGroupsQuery(Guid? TenantId) : IRequest<IReadOnlyList<GroupDto>>;

public sealed class ListGroupsQueryHandler(
    IApplicationDbContext db,
    ITenantContext tenantContext) : IRequestHandler<ListGroupsQuery, IReadOnlyList<GroupDto>>
{
    public async Task<IReadOnlyList<GroupDto>> Handle(ListGroupsQuery request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var groups = await db.Groups.AsNoTracking()
            .Where(g => !g.IsDeleted && (request.TenantId == null || g.TenantId == request.TenantId || g.TenantId == null))
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

        var result = new List<GroupDto>();
        foreach (var group in groups)
        {
            result.Add(await CreateGroupCommandHandler.MapGroupAsync(db, group.Id, cancellationToken));
        }

        return result;
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

        var exists = await db.Groups.AsNoTracking().AnyAsync(g => g.Id == request.Id && !g.IsDeleted, cancellationToken);
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
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Code)
            .Select(p => new PermissionDto(p.Id, p.Code, p.Name))
            .ToListAsync(cancellationToken);
    }
}
