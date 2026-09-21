using Formarca.Application.Common;
using Formarca.Application.Common.Models;
using Formarca.Application.Processes.Commands;
using Formarca.Application.Processes.Queries;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Processes;
using Formarca.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Formarca.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/processes")]
public sealed class ProcessesController(ISender sender) : ControllerBase
{
    public sealed record ChangeTrademarkStatusRequest(
        Guid TrademarkStatusId,
        string? Note,
        string? ProcessNumber);
    [Authorize(Policy = PermissionCodes.ProcessesRead)]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? tenantId,
        [FromQuery] string[]? tenantIds,
        [FromQuery] Guid? brandId,
        [FromQuery] string[]? brandIds,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(
            new ListProcessesQuery(
                QueryFilterValues.MergeGuids(tenantId, tenantIds),
                QueryFilterValues.MergeGuids(brandId, brandIds),
                page,
                pageSize),
            cancellationToken));

    [Authorize(Policy = PermissionCodes.ProcessesRead)]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(
        [FromQuery] Guid? tenantId,
        [FromQuery] string[]? tenantIds,
        [FromQuery] Guid? brandId,
        [FromQuery] string[]? brandIds,
        [FromQuery] string[]? funnelStage,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(
            new ListProcessesDashboardQuery(
                QueryFilterValues.MergeGuids(tenantId, tenantIds),
                QueryFilterValues.MergeGuids(brandId, brandIds),
                QueryFilterValues.NullIfEmpty(QueryFilterValues.ParseEnums<ProcessFunnelStage>(funnelStage)),
                page,
                pageSize),
            cancellationToken));

    [Authorize(Policy = PermissionCodes.ProcessesRead)]
    [HttpGet("trademark-dashboard")]
    public async Task<IActionResult> TrademarkDashboard(
        [FromQuery] Guid? tenantId,
        [FromQuery] string[]? tenantIds,
        [FromQuery] Guid? brandId,
        [FromQuery] string[]? brandIds,
        [FromQuery] Guid? statusId,
        [FromQuery] string[]? statusIds,
        [FromQuery] string[]? phase,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(
            new ListTrademarkDashboardQuery(
                QueryFilterValues.MergeGuids(tenantId, tenantIds),
                QueryFilterValues.MergeGuids(brandId, brandIds),
                QueryFilterValues.MergeGuids(statusId, statusIds),
                QueryFilterValues.NullIfEmpty(QueryFilterValues.ParseEnums<TrademarkStatusPhase>(phase)),
                page,
                pageSize),
            cancellationToken));

    [Authorize(Policy = PermissionCodes.ProcessesRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetProcessByIdQuery(id), cancellationToken));

    [Authorize(Policy = PermissionCodes.ProcessesRead)]
    [HttpGet("{id:guid}/trademark-status")]
    public async Task<IActionResult> TrademarkStatus(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetProcessTrademarkDetailQuery(id), cancellationToken));

    [Authorize(Policy = PermissionCodes.ProcessesManage)]
    [HttpPut("{id:guid}/trademark-status")]
    public async Task<IActionResult> ChangeTrademarkStatus(
        Guid id,
        [FromBody] ChangeTrademarkStatusRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new ChangeProcessTrademarkStatusCommand(
                id,
                request.TrademarkStatusId,
                request.Note,
                request.ProcessNumber),
            cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/trademark-statuses")]
public sealed class TrademarkStatusesController(ISender sender) : ControllerBase
{
    public sealed record UpsertTrademarkStatusRequest(
        string? Code,
        string Title,
        string Description,
        int? SortOrder,
        TrademarkStatusPhase Phase,
        TrademarkStatusTone Tone,
        bool IsActive);

    public sealed record ReorderTrademarkStatusesRequest(IReadOnlyList<Guid> OrderedIds);

    [Authorize(Policy = PermissionCodes.ProcessesRead)]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new ListTrademarkStatusesQuery(includeInactive), cancellationToken));

    [Authorize(Policy = AuthorizationExtensions.PlatformAdminPolicy)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] UpsertTrademarkStatusRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new CreateTrademarkStatusCommand(
                request.Code,
                request.Title,
                request.Description,
                request.Phase,
                request.Tone,
                request.SortOrder,
                request.IsActive),
            cancellationToken));

    [Authorize(Policy = AuthorizationExtensions.PlatformAdminPolicy)]
    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder(
        [FromBody] ReorderTrademarkStatusesRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new ReorderTrademarkStatusesCommand(request.OrderedIds), cancellationToken));

    [Authorize(Policy = AuthorizationExtensions.PlatformAdminPolicy)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpsertTrademarkStatusRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new UpdateTrademarkStatusCommand(
                id,
                request.Title,
                request.Description,
                request.SortOrder ?? 0,
                request.Phase,
                request.Tone,
                request.IsActive),
            cancellationToken));

    [Authorize(Policy = AuthorizationExtensions.PlatformAdminPolicy)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteTrademarkStatusCommand(id), cancellationToken);
        return NoContent();
    }
}
