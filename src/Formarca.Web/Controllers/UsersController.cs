using Formarca.Application.IdentityAccess.Users.Commands;
using Formarca.Application.IdentityAccess.Users.Queries;
using Formarca.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Formarca.Web.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationExtensions.PlatformAdminPolicy)]
[Route("api/admin/users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    public sealed record CreateUserRequest(
        string Email,
        string Password,
        string? DisplayName,
        bool IsPlatformAdmin,
        Guid? TenantId,
        Guid? GroupId);

    public sealed record AssignTenantRequest(Guid TenantId, bool IsDefault, Guid? GroupId);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
        => Ok(await sender.Send(new ListUsersQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetUserByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateUserCommand(
                request.Email,
                request.Password,
                request.DisplayName,
                request.IsPlatformAdmin,
                request.TenantId,
                request.GroupId),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/tenants")]
    public async Task<IActionResult> AssignTenant(Guid id, [FromBody] AssignTenantRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new AssignUserToTenantCommand(id, request.TenantId, request.IsDefault, request.GroupId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/tenants/{tenantId:guid}")]
    public async Task<IActionResult> RemoveTenant(Guid id, Guid tenantId, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveUserFromTenantCommand(id, tenantId), cancellationToken);
        return NoContent();
    }
}
