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
    public sealed record SubmitRegistrationRequest(
        string Name,
        string Email,
        string Phone,
        string Document,
        AddressDto Address,
        string? LegalRepresentativeName,
        string? LegalRepresentativeDocument,
        decimal? QuotedAmount,
        string? BudgetNotes,
        string? BrandName,
        string? BrandDescription);

    public sealed record UpdateRegistrationRequest(
        string Name,
        string Email,
        string Phone,
        string Document,
        AddressDto Address,
        string? LegalRepresentativeName,
        string? LegalRepresentativeDocument,
        decimal? QuotedAmount,
        string? BudgetNotes);

    [AllowAnonymous]
    [HttpPost("api/registrations")]
    public async Task<IActionResult> Submit([FromBody] SubmitRegistrationRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SubmitRegistrationCommand(
                request.Name,
                request.Email,
                request.Phone,
                request.Document,
                request.Address,
                request.BrandName,
                request.BrandDescription,
                request.LegalRepresentativeName,
                request.LegalRepresentativeDocument,
                request.QuotedAmount,
                request.BudgetNotes),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [Authorize(Policy = PermissionCodes.RegistrationsRead)]
    [HttpGet("api/registration")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCurrentRegistrationQuery(), cancellationToken));

    [Authorize(Policy = PermissionCodes.RegistrationsManage)]
    [HttpPut("api/registration")]
    public async Task<IActionResult> Update([FromBody] UpdateRegistrationRequest request, CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new UpdateRegistrationCommand(
                request.Name,
                request.Email,
                request.Phone,
                request.Document,
                request.Address,
                request.LegalRepresentativeName,
                request.LegalRepresentativeDocument,
                request.QuotedAmount,
                request.BudgetNotes),
            cancellationToken));

    [Authorize(Policy = PermissionCodes.RegistrationsRead)]
    [HttpGet("api/registration/{tenantId:guid}")]
    public async Task<IActionResult> GetByTenantId(Guid tenantId, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetRegistrationByTenantIdQuery(tenantId), cancellationToken));

    [Authorize(Policy = PermissionCodes.RegistrationsManage)]
    [HttpPut("api/registration/{tenantId:guid}")]
    public async Task<IActionResult> UpdateByTenantId(
        Guid tenantId,
        [FromBody] UpdateRegistrationRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new UpdateRegistrationByTenantIdCommand(
                tenantId,
                request.Name,
                request.Email,
                request.Phone,
                request.Document,
                request.Address,
                request.LegalRepresentativeName,
                request.LegalRepresentativeDocument,
                request.QuotedAmount,
                request.BudgetNotes),
            cancellationToken));
}
