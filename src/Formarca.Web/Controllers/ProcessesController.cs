using Formarca.Application.Common;
using Formarca.Application.Common.Models;
using Formarca.Application.Processes.Queries;
using Formarca.Domain.IdentityAccess;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Formarca.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/processes")]
public sealed class ProcessesController(ISender sender) : ControllerBase
{
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
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetProcessByIdQuery(id), cancellationToken));
}
