using Formarca.Application.Common;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.Contracts;
using Formarca.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.Contracts.Queries;

public sealed record ListContractsQuery(
    Guid? TenantId,
    Guid? BrandId,
    ContractStatus? Status,
    string? Code,
    string? Search,
    string? ClientName,
    string? BrandName,
    string? Document,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = Paging.DefaultPageSize) : IRequest<PagedResult<ContractDto>>;

public sealed class ListContractsQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<ListContractsQuery, PagedResult<ContractDto>>
{
    public async Task<PagedResult<ContractDto>> Handle(ListContractsQuery request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);

        var (page, pageSize, skip) = Paging.Normalize(request.Page, request.PageSize);
        var query = repository.Query<Contract>().Where(c => !c.IsDeleted);

        if (currentUser.IsPlatformAdmin)
        {
            if (request.TenantId is not null && request.TenantId != Guid.Empty)
            {
                query = query.Where(c => c.TenantId == request.TenantId);
            }
        }
        else
        {
            var tenantId = CommercialAccess.ResolveTenantId(currentUser, request.TenantId, requiredForAdmin: false);
            query = query.Where(c => c.TenantId == tenantId);
        }

        if (request.BrandId is not null && request.BrandId != Guid.Empty)
        {
            query = query.Where(c => c.BrandId == request.BrandId);
        }

        if (request.Status is not null)
        {
            query = query.Where(c => c.Status == request.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var code = request.Code.Trim();
            query = query.Where(c => c.Code.Contains(code));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(c => c.Code.Contains(search) || c.BrandName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.ClientName))
        {
            var clientName = request.ClientName.Trim();
            var tenantIds = repository.Query<Tenant>()
                .Where(t => !t.IsDeleted && t.Name.Contains(clientName))
                .Select(t => t.Id);
            query = query.Where(c => tenantIds.Contains(c.TenantId));
        }

        if (!string.IsNullOrWhiteSpace(request.BrandName))
        {
            var brandName = request.BrandName.Trim();
            query = query.Where(c => c.BrandName.Contains(brandName));
        }

        if (!string.IsNullOrWhiteSpace(request.Document))
        {
            var document = BrazilianDocuments.DigitsOnly(request.Document);
            if (document.Length > 0)
            {
                query = query.Where(c =>
                    c.CompanyDocument.Contains(document) || c.RepresentativeDocument.Contains(document));
            }
        }

        if (request.From is not null)
        {
            query = query.Where(c => c.GeneratedAtUtc >= request.From);
        }

        if (request.To is not null)
        {
            query = query.Where(c => c.GeneratedAtUtc <= request.To);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.GeneratedAtUtc)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ContractDto>(
            await CommercialMappings.MapAsync(repository, items, cancellationToken),
            total,
            page,
            pageSize);
    }
}

public sealed record GetContractByIdQuery(Guid Id) : IRequest<ContractDto>;

public sealed class GetContractByIdQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<GetContractByIdQuery, ContractDto>
{
    public async Task<ContractDto> Handle(GetContractByIdQuery request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);
        var contract = await repository.FirstOrDefaultAsync<Contract>(
            c => c.Id == request.Id && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Contract not found.");
        var mapped = await CommercialMappings.MapAsync(repository, [contract], cancellationToken);
        return mapped[0];
    }
}

public sealed record DownloadContractQuery(Guid Id, string Format) : IRequest<ContractDocumentFile>;

public sealed class DownloadContractQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext,
    IContractDocumentGenerator documents) : IRequestHandler<DownloadContractQuery, ContractDocumentFile>
{
    public async Task<ContractDocumentFile> Handle(DownloadContractQuery request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);
        var contract = await repository.FirstOrDefaultAsync<Contract>(
            c => c.Id == request.Id && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Contract not found.");

        if (!contract.CanDownload)
        {
            throw new ConflictException("Contract download is available after issuance.");
        }

        var format = (request.Format ?? "pdf").Trim().ToLowerInvariant();
        var model = new ContractDocumentModel(
            contract.Code,
            contract.CompanyDocument,
            contract.RepresentativeDocument,
            contract.BrandName,
            contract.Classes,
            contract.Amount,
            contract.GeneratedAtUtc,
            contract.FormarcaSignedAt,
            contract.ClientSignedAt,
            contract.Status.ToString());

        return format switch
        {
            "pdf" => new ContractDocumentFile($"{contract.Code}.pdf", "application/pdf", documents.GeneratePdf(model)),
            "docx" or "word" or "doc" => new ContractDocumentFile(
                $"{contract.Code}.docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                documents.GenerateWord(model)),
            _ => throw new ConflictException("Unsupported document format. Use pdf or docx.")
        };
    }
}
