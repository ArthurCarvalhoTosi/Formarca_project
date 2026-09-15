using Formarca.Application.Common;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.Contracts;
using Formarca.Domain.Contracts;
using Formarca.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.Contracts.Queries;

public sealed record ListContractsQuery(
    Guid? TenantId,
    Guid? BrandId,
    IReadOnlyList<ContractStatus>? Statuses,
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

        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses.ToList();
            query = query.Where(c => statuses.Contains(c.Status));
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

public sealed record PreviewContractQuery(Guid Id) : IRequest<ContractDocumentFile>;

public sealed class PreviewContractQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext,
    IContractDocumentGenerator documents,
    IAutentiqueClient autentique) : IRequestHandler<PreviewContractQuery, ContractDocumentFile>
{
    public async Task<ContractDocumentFile> Handle(PreviewContractQuery request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);
        var contract = await repository.FirstOrDefaultAsync<Contract>(
            c => c.Id == request.Id && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Contract not found.");

        if (!contract.CanPreview)
        {
            throw new ConflictException("Contract preview is available for pre-issued, issued, or signed contracts.");
        }

        if (contract.Status == ContractStatus.Signed)
        {
            var signed = await repository.FirstOrDefaultAsync<ContractFile>(
                f => f.ContractId == contract.Id && f.Kind == ContractFileKind.Signed && !f.IsDeleted,
                cancellationToken);
            if (signed is not null)
            {
                return new ContractDocumentFile(signed.FileName, signed.ContentType, signed.Content);
            }
        }

        if (!string.IsNullOrWhiteSpace(contract.AutentiqueDocumentId)
            && contract.Status is ContractStatus.Issued or ContractStatus.Signed)
        {
            byte[]? autentiquePdf = null;
            try
            {
                autentiquePdf = await autentique.TryDownloadCurrentPdfAsync(
                    contract.AutentiqueDocumentId,
                    cancellationToken);
            }
            catch (ConflictException)
            {
            }

            if (autentiquePdf is { Length: > 0 })
            {
                var kind = contract.PersistedFileKind;
                await ContractDocuments.UpsertPdfAsync(repository, contract, kind, autentiquePdf, cancellationToken);
                await repository.CommitAsync(cancellationToken);
                return new ContractDocumentFile($"{contract.Code}.pdf", "application/pdf", autentiquePdf);
            }
        }

        if (contract.Status == ContractStatus.Issued)
        {
            var issued = await repository.FirstOrDefaultAsync<ContractFile>(
                f => f.ContractId == contract.Id && f.Kind == ContractFileKind.Issued && !f.IsDeleted,
                cancellationToken);
            if (issued is not null)
            {
                return new ContractDocumentFile(issued.FileName, issued.ContentType, issued.Content);
            }
        }

        var pdf = documents.GeneratePdf(contract.ToDocumentModel(stampFormarcaSignature: false));
        return new ContractDocumentFile($"{contract.Code}-preview.pdf", "application/pdf", pdf);
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
        if (format is "docx" or "word" or "doc")
        {
            throw new ConflictException("Download is available as PDF only.");
        }

        if (format is not "pdf" and not "")
        {
            throw new ConflictException("Unsupported document format. Use pdf.");
        }

        var kind = contract.PersistedFileKind;
        var file = await repository.FirstOrDefaultAsync<ContractFile>(
            f => f.ContractId == contract.Id && f.Kind == kind && !f.IsDeleted,
            cancellationToken);

        if (file is null)
        {
            var pdf = documents.GeneratePdf(contract.ToDocumentModel(stampFormarcaSignature: false));
            await ContractDocuments.UpsertPdfAsync(repository, contract, kind, pdf, cancellationToken);
            await repository.CommitAsync(cancellationToken);
            return new ContractDocumentFile($"{contract.Code}.pdf", "application/pdf", pdf);
        }

        return new ContractDocumentFile(file.FileName, file.ContentType, file.Content);
    }
}

public sealed record GetContractSignLinkQuery(Guid Id) : IRequest<ContractSignLinkDto>;

public sealed class GetContractSignLinkQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext,
    IAutentiqueClient autentique) : IRequestHandler<GetContractSignLinkQuery, ContractSignLinkDto>
{
    public async Task<ContractSignLinkDto> Handle(GetContractSignLinkQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.IsPlatformAdmin)
        {
            throw new ForbiddenException("The client signs this contract in the tenant panel.");
        }

        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);
        var contract = await repository.FirstOrDefaultAsync<Contract>(
            c => c.Id == request.Id && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Contract not found.");

        if (contract.Status != ContractStatus.Issued)
        {
            throw new ConflictException("Signature link is available only for issued contracts.");
        }

        if (string.IsNullOrWhiteSpace(contract.AutentiqueSignerPublicId)
            && string.IsNullOrWhiteSpace(contract.AutentiqueSignUrl))
        {
            throw new ConflictException("This contract has no Autentique signature link.");
        }

        var signUrl = contract.AutentiqueSignUrl;
        if (!string.IsNullOrWhiteSpace(contract.AutentiqueSignerPublicId))
        {
            signUrl = await autentique.CreateLinkToSignatureAsync(contract.AutentiqueSignerPublicId, cancellationToken);
            contract.SetAutentiqueSignUrl(signUrl);
            await repository.CommitAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(signUrl))
        {
            throw new ConflictException("Could not obtain an Autentique signature link.");
        }

        return new ContractSignLinkDto(signUrl);
    }
}
