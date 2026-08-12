using Formarca.Application.IdentityAccess.Groups.Commands;
using Formarca.Application.IdentityAccess.Groups.Queries;
using Formarca.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Formarca.Web.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationExtensions.PlatformAdminPolicy)]
[Route("api/admin/groups")]
public sealed class GroupsController(ISender sender) : ControllerBase
{
    public sealed record CreateGroupRequest(string Name, Guid? TenantId, string? Description, List<string>? PermissionCodes);
    public sealed record UpdateGroupRequest(string Name, string? Description, List<string>? PermissionCodes);
    public sealed record AssignUserRequest(Guid UserId, Guid TenantId);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? tenantId, CancellationToken cancellationToken)
        => Ok(await sender.Send(new ListGroupsQuery(tenantId), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetGroupByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateGroupCommand(request.Name, request.TenantId, request.Description, request.PermissionCodes),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGroupRequest request, CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new UpdateGroupCommand(id, request.Name, request.Description, request.PermissionCodes),
            cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteGroupCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/users")]
    public async Task<IActionResult> AssignUser(Guid id, [FromBody] AssignUserRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new AssignUserToGroupCommand(request.UserId, id, request.TenantId), cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize(Policy = AuthorizationExtensions.PlatformAdminPolicy)]
[Route("api/admin/permissions")]
public sealed class PermissionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
        => Ok(await sender.Send(new ListPermissionsQuery(), cancellationToken));
}
