using Formarca.Application.Common.Models;
using Formarca.Application.Tenancy.Commands;
using Formarca.Application.Tenancy.Queries;
using Formarca.Domain.IdentityAccess;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Formarca.Web.Controllers;

[ApiController]
public sealed class RegistrationController(ISender sender) : ControllerBase
{
    public sealed record RegistrationRequest(
        string Name,
        string Email,
        string Phone,
        string Document,
        AddressDto Address,
        string BrandName,
        string BrandDescription);

    [AllowAnonymous]
    [HttpPost("api/registrations")]
    public async Task<IActionResult> Submit([FromBody] RegistrationRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SubmitRegistrationCommand(
                request.Name,
                request.Email,
                request.Phone,
                request.Document,
                request.Address,
                request.BrandName,
                request.BrandDescription),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [Authorize(Policy = PermissionCodes.RegistrationsRead)]
    [HttpGet("api/registration")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCurrentRegistrationQuery(), cancellationToken));

    [Authorize(Policy = PermissionCodes.RegistrationsManage)]
    [HttpPut("api/registration")]
    public async Task<IActionResult> Update([FromBody] RegistrationRequest request, CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new UpdateRegistrationCommand(
                request.Name,
                request.Email,
                request.Phone,
                request.Document,
                request.Address,
                request.BrandName,
                request.BrandDescription),
            cancellationToken));

    [Authorize(Policy = PermissionCodes.RegistrationsRead)]
    [HttpGet("api/registration/{tenantId:guid}")]
    public async Task<IActionResult> GetByTenantId(Guid tenantId, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetRegistrationByTenantIdQuery(tenantId), cancellationToken));

    [Authorize(Policy = PermissionCodes.RegistrationsManage)]
    [HttpPut("api/registration/{tenantId:guid}")]
    public async Task<IActionResult> UpdateByTenantId(
        Guid tenantId,
        [FromBody] RegistrationRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new UpdateRegistrationByTenantIdCommand(
                tenantId,
                request.Name,
                request.Email,
                request.Phone,
                request.Document,
                request.Address,
                request.BrandName,
                request.BrandDescription),
            cancellationToken));
}
