using Formarca.Application.Common;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.Processes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcessEntity = Formarca.Domain.Processes.Process;

namespace Formarca.Application.Processes.Queries;

public sealed record ListProcessesQuery(
    Guid? TenantId,
    Guid? BrandId,
    int Page = 1,
    int PageSize = Paging.DefaultPageSize) : IRequest<PagedResult<ProcessDto>>;

public sealed class ListProcessesQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<ListProcessesQuery, PagedResult<ProcessDto>>
{
    public async Task<PagedResult<ProcessDto>> Handle(ListProcessesQuery request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);

        var (page, pageSize, skip) = Paging.Normalize(request.Page, request.PageSize);
        var query = repository.Query<ProcessEntity>().Where(p => !p.IsDeleted);

        if (currentUser.IsPlatformAdmin)
        {
            if (request.TenantId is not null && request.TenantId != Guid.Empty)
            {
                query = query.Where(p => p.TenantId == request.TenantId);
            }
        }
        else
        {
            var tenantId = CommercialAccess.ResolveTenantId(currentUser, request.TenantId, requiredForAdmin: false);
            query = query.Where(p => p.TenantId == tenantId);
        }

        if (request.BrandId is not null && request.BrandId != Guid.Empty)
        {
            query = query.Where(p => p.BrandId == request.BrandId);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProcessDto>(
            await CommercialMappings.MapAsync(repository, items, cancellationToken),
            total,
            page,
            pageSize);
    }
}

public sealed record GetProcessByIdQuery(Guid Id) : IRequest<ProcessDto>;

public sealed class GetProcessByIdQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<GetProcessByIdQuery, ProcessDto>
{
    public async Task<ProcessDto> Handle(GetProcessByIdQuery request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);
        var process = await repository.FirstOrDefaultAsync<ProcessEntity>(
            p => p.Id == request.Id && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Process not found.");
        var mapped = await CommercialMappings.MapAsync(repository, [process], cancellationToken);
        return mapped[0];
    }
}
