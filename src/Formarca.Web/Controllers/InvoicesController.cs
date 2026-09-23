using Formarca.Application.Common;
using Formarca.Application.Common.Models;
using Formarca.Application.Invoices.Commands;
using Formarca.Application.Invoices.Queries;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Invoices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Formarca.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/invoices")]
public sealed class InvoicesController(ISender sender) : ControllerBase
{
    public sealed record SubmitPaymentRequest(PaymentMethod PaymentMethod, int Installments);

    public sealed record ApproveInvoiceRequest(decimal? ReceivedAmount);

    [Authorize(Policy = PermissionCodes.InvoicesRead)]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? tenantId,
        [FromQuery] string[]? tenantIds,
        [FromQuery] Guid? contractId,
        [FromQuery] string[]? status,
        [FromQuery] DateTime? activeSince,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(
            new ListInvoicesQuery(
                tenantId,
                QueryFilterValues.MergeGuids(tenantId, tenantIds),
                contractId,
                QueryFilterValues.NullIfEmpty(QueryFilterValues.ParseEnums<InvoiceStatus>(status)),
                page,
                pageSize,
                activeSince),
            cancellationToken));

    [Authorize(Policy = PermissionCodes.InvoicesRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetInvoiceByIdQuery(id), cancellationToken));

    [Authorize(Policy = PermissionCodes.InvoicesRead)]
    [HttpGet("by-contract/{contractId:guid}")]
    public async Task<IActionResult> GetByContract(Guid contractId, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetInvoiceByContractQuery(contractId), cancellationToken));

    [Authorize(Policy = PermissionCodes.InvoicesPay)]
    [HttpPost("{id:guid}/submit-payment")]
    public async Task<IActionResult> SubmitPayment(
        Guid id,
        [FromBody] SubmitPaymentRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new SubmitInvoicePaymentCommand(id, request.PaymentMethod, request.Installments),
            cancellationToken));

    [Authorize(Policy = PermissionCodes.InvoicesManage)]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody] ApproveInvoiceRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new ApproveInvoiceCommand(id, request.ReceivedAmount), cancellationToken));

    [Authorize(Policy = PermissionCodes.InvoicesManage)]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new RejectInvoiceCommand(id), cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/admin/invoice-settings")]
public sealed class InvoiceSettingsController(ISender sender) : ControllerBase
{
    public sealed record UpdateInvoiceSettingsRequest(
        Dictionary<string, decimal>? InterestRates,
        string? InterestRatesJson,
        decimal PixDiscount,
        decimal GeneralDiscount);

    [Authorize(Policy = PermissionCodes.InvoicesRead)]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetInvoiceSettingsQuery(), cancellationToken));

    [Authorize(Policy = Formarca.Web.Authorization.AuthorizationExtensions.PlatformAdminPolicy)]
    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UpdateInvoiceSettingsRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new UpdateInvoiceSettingsCommand(
                request.InterestRates,
                request.InterestRatesJson,
                request.PixDiscount,
                request.GeneralDiscount),
            cancellationToken));
}
