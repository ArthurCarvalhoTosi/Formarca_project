using Formarca.Application.Tenancy.Commands;
using Formarca.Application.Tenancy.Queries;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Tenancy;
using Formarca.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Formarca.Web.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationExtensions.PlatformAdminPolicy)]
[Route("api/admin/tenants")]
public sealed class TenantsController(ISender sender) : ControllerBase
{
    public sealed record CreateTenantRequest(string Name, string Slug, string? Document);
    public sealed record UpdateTenantRequest(string Name, string? Document, TenantStatus Status);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
        => Ok(await sender.Send(new ListTenantsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetTenantByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateTenantCommand(request.Name, request.Slug, request.Document), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantRequest request, CancellationToken cancellationToken)
        => Ok(await sender.Send(new UpdateTenantCommand(id, request.Name, request.Document, request.Status), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteTenantCommand(id), cancellationToken);
        return NoContent();
    }
}
