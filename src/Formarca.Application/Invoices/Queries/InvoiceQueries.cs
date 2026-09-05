using Formarca.Application.Common;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.Invoices.Commands;
using Formarca.Domain.Contracts;
using Formarca.Domain.Invoices;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

namespace Formarca.Application.Invoices.Queries;

public sealed record ListInvoicesQuery(
    Guid? TenantId,
    Guid? ContractId,
    InvoiceStatus? Status,
    int Page = 1,
    int PageSize = Paging.DefaultPageSize) : IRequest<PagedResult<InvoiceDto>>;

public sealed class ListInvoicesQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<ListInvoicesQuery, PagedResult<InvoiceDto>>
{
    public async Task<PagedResult<InvoiceDto>> Handle(ListInvoicesQuery request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);

        var (page, pageSize, skip) = Paging.Normalize(request.Page, request.PageSize);
        var query = repository.Query<Invoice>().Where(i => !i.IsDeleted);

        if (currentUser.IsPlatformAdmin)
        {
            if (request.TenantId is not null && request.TenantId != Guid.Empty)
            {
                query = query.Where(i => i.TenantId == request.TenantId);
            }
        }
        else
        {
            var tenantId = CommercialAccess.ResolveTenantId(currentUser, request.TenantId, requiredForAdmin: false);
            query = query.Where(i => i.TenantId == tenantId);
        }

        if (request.ContractId is not null && request.ContractId != Guid.Empty)
        {
            query = query.Where(i => i.ContractId == request.ContractId);
        }

        if (request.Status is not null)
        {
            query = query.Where(i => i.Status == request.Status);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<InvoiceDto>(
            await CommercialMappings.MapAsync(repository, items, cancellationToken),
            total,
            page,
            pageSize);
    }
}

public sealed record GetInvoiceByIdQuery(Guid Id) : IRequest<InvoiceDto>;

public sealed class GetInvoiceByIdQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<GetInvoiceByIdQuery, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);
        var invoice = await repository.FirstOrDefaultAsync<Invoice>(
            i => i.Id == request.Id && !i.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Invoice not found.");
        var mapped = await CommercialMappings.MapAsync(repository, [invoice], cancellationToken);
        return mapped[0];
    }
}

public sealed record GetInvoiceByContractQuery(Guid ContractId) : IRequest<InvoiceDto>;

public sealed class GetInvoiceByContractQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<GetInvoiceByContractQuery, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(GetInvoiceByContractQuery request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);
        var invoice = await repository.FirstOrDefaultAsync<Invoice>(
            i => i.ContractId == request.ContractId && !i.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Invoice not found.");
        var mapped = await CommercialMappings.MapAsync(repository, [invoice], cancellationToken);
        return mapped[0];
    }
}

public sealed record GetInvoiceSettingsQuery : IRequest<InvoiceSettingsDto>;

public sealed class GetInvoiceSettingsQueryHandler(
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<GetInvoiceSettingsQuery, InvoiceSettingsDto>
{
    public async Task<InvoiceSettingsDto> Handle(GetInvoiceSettingsQuery request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);
        var settings = await InvoiceCommands.LoadSettingsAsync(
            repository, cancellationToken, createIfMissing: true);
        return settings.ToDto();
    }
}
