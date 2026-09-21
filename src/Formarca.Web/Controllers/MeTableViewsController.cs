using Formarca.Application.IdentityAccess.TableViews.Commands;
using Formarca.Application.IdentityAccess.TableViews.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Formarca.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/me/table-views")]
public sealed class MeTableViewsController(ISender sender) : ControllerBase
{
    public sealed record CreateTableViewRequest(
        string TableKey,
        string Name,
        List<string> ColumnKeys,
        bool? Activate);

    public sealed record UpdateTableViewRequest(string? Name, List<string>? ColumnKeys);

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string tableKey,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new ListUserTableViewsQuery(tableKey), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTableViewRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new CreateUserTableViewCommand(
                request.TableKey,
                request.Name,
                request.ColumnKeys,
                request.Activate ?? false),
            cancellationToken);
        return Created($"/api/me/table-views/{result.Id}", result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTableViewRequest request,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(
            new UpdateUserTableViewCommand(id, request.Name, request.ColumnKeys),
            cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new DeleteUserTableViewCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new ActivateUserTableViewCommand(id), cancellationToken));
}
