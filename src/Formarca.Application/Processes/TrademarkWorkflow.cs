using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Domain.Brands;
using Formarca.Domain.Contracts;
using Formarca.Domain.Processes;
using Formarca.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using ProcessEntity = Formarca.Domain.Processes.Process;

namespace Formarca.Application.Processes;

internal static class TrademarkWorkflow
{
    public static TrademarkStatusDto ToDto(this TrademarkStatus status) =>
        new(
            status.Id,
            status.Code,
            status.Title,
            status.Description,
            status.SortOrder,
            status.Phase,
            status.Tone,
            status.IsActive,
            status.CreatedAtUtc,
            status.UpdatedAtUtc);

    public static void EnsurePlatformAdmin(ICurrentUser currentUser)
    {
        if (!currentUser.IsPlatformAdmin)
        {
            throw new ForbiddenException("Only platform administrators can manage trademark statuses.");
        }
    }

    public static async Task<TrademarkStatus> LoadStatusAsync(
        IGenericRepository repository,
        Guid id,
        CancellationToken cancellationToken)
    {
        return await repository.FirstOrDefaultAsync<TrademarkStatus>(
            status => status.Id == id && !status.IsDeleted,
            cancellationToken)
            ?? throw new NotFoundException("Trademark status not found.");
    }

    public static async Task<ProcessTrademarkDetailDto> BuildDetailAsync(
        IGenericRepository repository,
        ProcessEntity process,
        CancellationToken cancellationToken)
    {
        var brand = await repository.FirstOrDefaultAsync<Brand>(
            item => item.Id == process.BrandId,
            cancellationToken);
        var tenant = await repository.FirstOrDefaultAsync<Tenant>(
            item => item.Id == process.TenantId,
            cancellationToken);
        var contract = await repository.FirstOrDefaultAsync<Contract>(
            item => item.Id == process.ContractId,
            cancellationToken);

        TrademarkStatus? current = null;
        if (process.TrademarkStatusId is Guid statusId)
        {
            current = await repository.FirstOrDefaultAsync<TrademarkStatus>(
                status => status.Id == statusId,
                cancellationToken);
        }

        var changes = await repository.Query<ProcessStatusChange>()
            .Where(change => change.ProcessId == process.Id && !change.IsDeleted)
            .OrderByDescending(change => change.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var statusIds = changes
            .SelectMany(change => new[] { change.FromStatusId, change.ToStatusId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var statuses = statusIds.Count == 0
            ? new Dictionary<Guid, TrademarkStatus>()
            : await repository.Query<TrademarkStatus>()
                .Where(status => statusIds.Contains(status.Id))
                .ToDictionaryAsync(status => status.Id, cancellationToken);

        var history = changes.Select(change =>
        {
            TrademarkStatus? from = null;
            if (change.FromStatusId is Guid fromId)
            {
                statuses.TryGetValue(fromId, out from);
            }

            statuses.TryGetValue(change.ToStatusId, out var to);
            return new ProcessStatusChangeDto(
                change.Id,
                change.FromStatusId,
                from?.Title,
                change.ToStatusId,
                to?.Title ?? "Removed status",
                change.Note,
                change.ChangedByUserId,
                change.CreatedAtUtc);
        }).ToList();

        return new ProcessTrademarkDetailDto(
            process.Id,
            process.BrandId,
            brand?.Name ?? string.Empty,
            process.TenantId,
            tenant?.Name,
            process.ContractId,
            contract?.Code,
            process.ProcessNumber,
            current?.ToDto(),
            process.TrademarkStatusChangedAtUtc,
            history);
    }
}
