using Formarca.Application.Brands.Commands;
using Formarca.Application.Brands.Queries;
using Formarca.Application.Common.Models;
using Formarca.Domain.IdentityAccess;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Formarca.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/brands")]
public sealed class BrandsController(ISender sender) : ControllerBase
{
    public sealed record BrandRequest(
        Guid TenantId,
        string Name,
        string ServicesDescription,
        List<int>? InpiClasses);

    public sealed record UpdateBrandRequest(
        string Name,
        string ServicesDescription,
        List<int>? InpiClasses);

    [Authorize(Policy = PermissionCodes.BrandsRead)]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? tenantId,
        [FromQuery] string? name,
        [FromQuery] int? inpiClass,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new ListBrandsQuery(tenantId, name, inpiClass, page, pageSize), cancellationToken));

    [Authorize(Policy = PermissionCodes.BrandsRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetBrandByIdQuery(id), cancellationToken));

    [Authorize(Policy = PermissionCodes.BrandsManage)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BrandRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateBrandCommand(request.TenantId, request.Name, request.ServicesDescription, request.InpiClasses),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [Authorize(Policy = PermissionCodes.BrandsManage)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBrandRequest request, CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new UpdateBrandCommand(id, request.Name, request.ServicesDescription, request.InpiClasses),
            cancellationToken));

    [Authorize(Policy = PermissionCodes.BrandsManage)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteBrandCommand(id), cancellationToken);
        return NoContent();
    }
}
