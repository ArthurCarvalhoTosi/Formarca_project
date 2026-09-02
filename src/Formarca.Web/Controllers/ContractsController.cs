using Formarca.Application.Common.Models;
using Formarca.Application.Contracts.Commands;
using Formarca.Application.Contracts.Queries;
using Formarca.Domain.Contracts;
using Formarca.Domain.IdentityAccess;
using Formarca.Web.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Formarca.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/contracts")]
public sealed class ContractsController(ISender sender) : ControllerBase
{
    public sealed record PreIssueContractRequest(
        Guid BrandId,
        decimal Amount,
        string? CompanyDocument,
        string? RepresentativeDocument,
        string? BrandName,
        [property: JsonConverter(typeof(ContractClassesConverter))] string? Classes);

    public sealed record UpdateContractRequest(
        decimal Amount,
        string CompanyDocument,
        string RepresentativeDocument,
        string BrandName,
        [property: JsonConverter(typeof(ContractClassesConverter))] string Classes,
        string? BodyMarkdown,
        string? MinimumTerm,
        string? CompanyName,
        string? Street,
        string? Number,
        string? City,
        string? State,
        string? ZipCode);

    public sealed record SignContractRequest(string SignaturePngBase64);

    [Authorize(Policy = PermissionCodes.ContractsRead)]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? brandId,
        [FromQuery] ContractStatus? status,
        [FromQuery] string? code,
        [FromQuery] string? search,
        [FromQuery] string? clientName,
        [FromQuery] string? brandName,
        [FromQuery] string? document,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(
            new ListContractsQuery(
                tenantId,
                brandId,
                status,
                code,
                search,
                clientName,
                brandName,
                document,
                from,
                to,
                page,
                pageSize),
            cancellationToken));

    [Authorize(Policy = PermissionCodes.ContractsRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetContractByIdQuery(id), cancellationToken));

    [Authorize(Policy = PermissionCodes.ContractsManage)]
    [HttpPost("pre-issue")]
    public async Task<IActionResult> PreIssue([FromBody] PreIssueContractRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new PreIssueContractCommand(
                request.BrandId,
                request.Amount,
                request.CompanyDocument,
                request.RepresentativeDocument,
                request.BrandName,
                request.Classes),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [Authorize(Policy = PermissionCodes.ContractsManage)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContractRequest request, CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new UpdateContractCommand(
                id,
                request.Amount,
                request.CompanyDocument,
                request.RepresentativeDocument,
                request.BrandName,
                request.Classes,
                request.BodyMarkdown,
                request.MinimumTerm,
                request.CompanyName,
                request.Street,
                request.Number,
                request.City,
                request.State,
                request.ZipCode),
            cancellationToken));

    [Authorize(Policy = PermissionCodes.ContractsManage)]
    [HttpPost("{id:guid}/issue")]
    public async Task<IActionResult> Issue(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new IssueContractCommand(id), cancellationToken));

    [Authorize(Policy = PermissionCodes.ContractsRead)]
    [HttpGet("{id:guid}/sign-link")]
    public async Task<IActionResult> SignLink(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetContractSignLinkQuery(id), cancellationToken));

    [Authorize(Policy = PermissionCodes.ContractsSign)]
    [HttpPost("{id:guid}/sign")]
    public async Task<IActionResult> Sign(Guid id, [FromBody] SignContractRequest? request, CancellationToken cancellationToken)
        => Ok(await sender.Send(new SignContractCommand(id, request?.SignaturePngBase64), cancellationToken));

    [Authorize(Policy = PermissionCodes.ContractsManage)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new CancelContractCommand(id), cancellationToken));

    [Authorize(Policy = PermissionCodes.ContractsRead)]
    [HttpGet("{id:guid}/preview")]
    public async Task<IActionResult> Preview(Guid id, CancellationToken cancellationToken)
    {
        var file = await sender.Send(new PreviewContractQuery(id), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize(Policy = PermissionCodes.ContractsRead)]
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(
        Guid id,
        [FromQuery] string format = "pdf",
        CancellationToken cancellationToken = default)
    {
        var file = await sender.Send(new DownloadContractQuery(id, format), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
