using Formarca.Application.Common;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.Brands;
using Formarca.Domain.Processes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcessEntity = Formarca.Domain.Processes.Process;

namespace Formarca.Application.Processes.Queries;

public sealed record ListProcessesQuery(
    IReadOnlyList<Guid>? TenantIds,
    IReadOnlyList<Guid>? BrandIds,
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

        var tenantIds = request.TenantIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? [];
        CommercialAccess.EnsureRequestedTenantsAllowed(currentUser, tenantIds);

        if (currentUser.IsPlatformAdmin)
        {
            if (tenantIds.Count > 0)
            {
                query = query.Where(p => tenantIds.Contains(p.TenantId));
            }
        }
        else
        {
            var tenantId = CommercialAccess.ResolveTenantId(
                currentUser,
                tenantIds.Count == 1 ? tenantIds[0] : null,
                requiredForAdmin: false);
            query = query.Where(p => p.TenantId == tenantId);
        }

        if (request.BrandIds is { Count: > 0 })
        {
            var brandIds = request.BrandIds.Where(id => id != Guid.Empty).Distinct().ToList();
            if (brandIds.Count > 0)
            {
                query = query.Where(p => brandIds.Contains(p.BrandId));
            }
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

public sealed record ListProcessesDashboardQuery(
    IReadOnlyList<Guid>? TenantIds,
    IReadOnlyList<Guid>? BrandIds,
    IReadOnlyList<ProcessFunnelStage>? FunnelStages,
    int Page = 1,
    int PageSize = Paging.DefaultPageSize) : IRequest<ProcessDashboardResult>;

public sealed class ListProcessesDashboardQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<ListProcessesDashboardQuery, ProcessDashboardResult>
{
    public async Task<ProcessDashboardResult> Handle(
        ListProcessesDashboardQuery request,
        CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);

        var (page, pageSize, skip) = Paging.Normalize(request.Page, request.PageSize);
        var query = repository.Query<Brand>().Where(b => !b.IsDeleted);

        var tenantIds = request.TenantIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? [];
        CommercialAccess.EnsureRequestedTenantsAllowed(currentUser, tenantIds);

        if (currentUser.IsPlatformAdmin)
        {
            if (tenantIds.Count > 0)
            {
                query = query.Where(b => tenantIds.Contains(b.TenantId));
            }
        }
        else
        {
            var tenantId = CommercialAccess.ResolveTenantId(
                currentUser,
                tenantIds.Count == 1 ? tenantIds[0] : null,
                requiredForAdmin: false);
            query = query.Where(b => b.TenantId == tenantId);
        }

        if (request.BrandIds is { Count: > 0 })
        {
            var brandIds = request.BrandIds.Where(id => id != Guid.Empty).Distinct().ToList();
            if (brandIds.Count > 0)
            {
                query = query.Where(b => brandIds.Contains(b.Id));
            }
        }

        var brands = await query
            .OrderBy(b => b.Name)
            .ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);

        var cards = await CommercialMappings.MapDashboardAsync(repository, brands, cancellationToken);
        var kpis = CommercialMappings.ToDashboardKpis(cards);

        IEnumerable<ProcessDashboardItemDto> filtered = cards;
        if (request.FunnelStages is { Count: > 0 })
        {
            var stages = request.FunnelStages.ToList();
            filtered = cards.Where(item => stages.Contains(item.FunnelStage));
        }

        var matching = filtered.ToList();
        var items = matching.Skip(skip).Take(pageSize).ToList();

        return new ProcessDashboardResult(items, kpis, matching.Count, page, pageSize);
    }
}
