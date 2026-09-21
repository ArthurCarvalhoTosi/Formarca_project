using Formarca.Application.Common;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.Brands;
using Formarca.Domain.Contracts;
using Formarca.Domain.Processes;
using Formarca.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcessEntity = Formarca.Domain.Processes.Process;

namespace Formarca.Application.Processes.Queries;

public sealed record ListTrademarkDashboardQuery(
    IReadOnlyList<Guid>? TenantIds,
    IReadOnlyList<Guid>? BrandIds,
    IReadOnlyList<Guid>? StatusIds,
    IReadOnlyList<TrademarkStatusPhase>? Phases,
    int Page = 1,
    int PageSize = Paging.DefaultPageSize) : IRequest<TrademarkDashboardResult>;

public sealed class ListTrademarkDashboardQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<ListTrademarkDashboardQuery, TrademarkDashboardResult>
{
    public async Task<TrademarkDashboardResult> Handle(
        ListTrademarkDashboardQuery request,
        CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);

        var (page, pageSize, skip) = Paging.Normalize(request.Page, request.PageSize);
        var query = repository.Query<ProcessEntity>().Where(process => !process.IsDeleted);

        var tenantIds = request.TenantIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? [];
        CommercialAccess.EnsureRequestedTenantsAllowed(currentUser, tenantIds);

        if (currentUser.IsPlatformAdmin)
        {
            if (tenantIds.Count > 0)
            {
                query = query.Where(process => tenantIds.Contains(process.TenantId));
            }
        }
        else
        {
            var tenantId = CommercialAccess.ResolveTenantId(
                currentUser,
                tenantIds.Count == 1 ? tenantIds[0] : null,
                requiredForAdmin: false);
            query = query.Where(process => process.TenantId == tenantId);
        }

        if (request.BrandIds is { Count: > 0 })
        {
            var brandIds = request.BrandIds.Where(id => id != Guid.Empty).Distinct().ToList();
            if (brandIds.Count > 0)
            {
                query = query.Where(process => brandIds.Contains(process.BrandId));
            }
        }

        var processes = await query.ToListAsync(cancellationToken);
        var statuses = await repository.Query<TrademarkStatus>()
            .Where(status => !status.IsDeleted)
            .ToListAsync(cancellationToken);
        var statusById = statuses.ToDictionary(status => status.Id);
        var kpis = BuildKpis(processes, statusById);

        IEnumerable<ProcessEntity> filtered = processes;
        if (request.StatusIds is { Count: > 0 })
        {
            var statusIds = request.StatusIds.ToHashSet();
            filtered = filtered.Where(process =>
                process.TrademarkStatusId is Guid statusId && statusIds.Contains(statusId));
        }

        if (request.Phases is { Count: > 0 })
        {
            var phases = request.Phases.ToHashSet();
            filtered = filtered.Where(process =>
                process.TrademarkStatusId is Guid statusId
                && statusById.TryGetValue(statusId, out var status)
                && phases.Contains(status.Phase));
        }

        var matching = filtered
            .OrderByDescending(process => process.TrademarkStatusChangedAtUtc ?? process.CreatedAtUtc)
            .ThenBy(process => process.Id)
            .ToList();
        var pageItems = matching.Skip(skip).Take(pageSize).ToList();
        var items = await MapItemsAsync(repository, pageItems, statusById, cancellationToken);

        return new TrademarkDashboardResult(items, kpis, matching.Count, page, pageSize);
    }

    private static TrademarkDashboardKpisDto BuildKpis(
        IReadOnlyList<ProcessEntity> processes,
        IReadOnlyDictionary<Guid, TrademarkStatus> statusById)
    {
        var phaseCounts = Enum.GetValues<TrademarkStatusPhase>()
            .ToDictionary(phase => phase, _ => 0);
        var withoutStatus = 0;
        var actionRequired = 0;
        var paused = 0;
        var activeRights = 0;
        var closed = 0;

        foreach (var process in processes)
        {
            if (process.TrademarkStatusId is not Guid statusId
                || !statusById.TryGetValue(statusId, out var status))
            {
                withoutStatus++;
                continue;
            }

            phaseCounts[status.Phase]++;
            switch (status.Tone)
            {
                case TrademarkStatusTone.Attention:
                    actionRequired++;
                    break;
                case TrademarkStatusTone.Paused:
                    paused++;
                    break;
                case TrademarkStatusTone.Danger:
                    closed++;
                    break;
                case TrademarkStatusTone.Success
                    when status.Phase is TrademarkStatusPhase.Grant or TrademarkStatusPhase.Renewal:
                    activeRights++;
                    break;
            }
        }

        return new TrademarkDashboardKpisDto(
            processes.Count,
            withoutStatus,
            actionRequired,
            paused,
            activeRights,
            closed,
            phaseCounts
                .OrderBy(pair => pair.Key)
                .Select(pair => new TrademarkPhaseCountDto(pair.Key, pair.Value))
                .ToList());
    }

    private static async Task<IReadOnlyList<TrademarkDashboardItemDto>> MapItemsAsync(
        IGenericRepository repository,
        IReadOnlyList<ProcessEntity> processes,
        IReadOnlyDictionary<Guid, TrademarkStatus> statusById,
        CancellationToken cancellationToken)
    {
        if (processes.Count == 0)
        {
            return [];
        }

        var brandIds = processes.Select(process => process.BrandId).Distinct().ToList();
        var brands = await repository.Query<Brand>()
            .Where(brand => brandIds.Contains(brand.Id))
            .ToDictionaryAsync(brand => brand.Id, brand => brand.Name, cancellationToken);

        var tenantIds = processes.Select(process => process.TenantId).Distinct().ToList();
        var tenants = await repository.Query<Tenant>()
            .Where(tenant => tenantIds.Contains(tenant.Id))
            .ToDictionaryAsync(tenant => tenant.Id, tenant => tenant.Name, cancellationToken);

        var contractIds = processes.Select(process => process.ContractId).Distinct().ToList();
        var contracts = await repository.Query<Contract>()
            .Where(contract => contractIds.Contains(contract.Id))
            .ToDictionaryAsync(contract => contract.Id, contract => contract.Code, cancellationToken);

        var processIds = processes.Select(process => process.Id).ToList();
        var changes = await repository.Query<ProcessStatusChange>()
            .Where(change => processIds.Contains(change.ProcessId) && !change.IsDeleted)
            .ToListAsync(cancellationToken);
        var latestNote = changes
            .GroupBy(change => change.ProcessId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(change => change.CreatedAtUtc).First().Note);

        return processes.Select(process =>
        {
            brands.TryGetValue(process.BrandId, out var brandName);
            tenants.TryGetValue(process.TenantId, out var tenantName);
            contracts.TryGetValue(process.ContractId, out var contractCode);
            latestNote.TryGetValue(process.Id, out var note);
            TrademarkStatus? status = null;
            if (process.TrademarkStatusId is Guid statusId)
            {
                statusById.TryGetValue(statusId, out status);
            }

            return new TrademarkDashboardItemDto(
                process.Id,
                process.BrandId,
                brandName ?? string.Empty,
                process.TenantId,
                tenantName,
                process.ContractId,
                contractCode,
                process.ProcessNumber,
                process.CreatedAtUtc,
                status?.ToDto(),
                process.TrademarkStatusChangedAtUtc,
                note);
        }).ToList();
    }
}

public sealed record GetProcessTrademarkDetailQuery(Guid ProcessId) : IRequest<ProcessTrademarkDetailDto>;

public sealed class GetProcessTrademarkDetailQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<GetProcessTrademarkDetailQuery, ProcessTrademarkDetailDto>
{
    public async Task<ProcessTrademarkDetailDto> Handle(
        GetProcessTrademarkDetailQuery request,
        CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);
        var process = await repository.FirstOrDefaultAsync<ProcessEntity>(
            item => item.Id == request.ProcessId && !item.IsDeleted,
            cancellationToken)
            ?? throw new NotFoundException("Process not found.");
        CommercialAccess.EnsureRequestedTenantsAllowed(currentUser, [process.TenantId]);
        return await TrademarkWorkflow.BuildDetailAsync(repository, process, cancellationToken);
    }
}
