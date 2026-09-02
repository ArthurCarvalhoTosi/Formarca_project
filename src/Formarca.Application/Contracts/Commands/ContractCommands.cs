using FluentValidation;
using Formarca.Application.Common;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.Brands;
using Formarca.Domain.Common;
using Formarca.Domain.Contracts;
using Formarca.Domain.Invoices;
using Formarca.Domain.Tenancy;
using MediatR;

namespace Formarca.Application.Contracts.Commands;

public sealed record PreIssueContractCommand(
    Guid BrandId,
    decimal Amount,
    string? CompanyDocument,
    string? RepresentativeDocument,
    string? BrandName,
    string? Classes) : IRequest<ContractDto>;

public sealed class PreIssueContractCommandValidator : AbstractValidator<PreIssueContractCommand>
{
    public PreIssueContractCommandValidator()
    {
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CompanyDocument).MaximumLength(32);
        RuleFor(x => x.RepresentativeDocument).MaximumLength(32);
        RuleFor(x => x.BrandName).MaximumLength(200);
        RuleFor(x => x.Classes).MaximumLength(200);
    }
}

public sealed class PreIssueContractCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext,
    IContractTemplateProvider templates) : IRequestHandler<PreIssueContractCommand, ContractDto>
{
    public async Task<ContractDto> Handle(PreIssueContractCommand request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);

        var brand = await repository.FirstOrDefaultAsync<Brand>(
            b => b.Id == request.BrandId && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Brand not found.");

        var tenant = await repository.FirstOrDefaultAsync<Tenant>(
            t => t.Id == brand.TenantId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        var companyDocument = string.IsNullOrWhiteSpace(request.CompanyDocument)
            ? tenant.Document
            : request.CompanyDocument;
        var representativeDocument = string.IsNullOrWhiteSpace(request.RepresentativeDocument)
            ? tenant.LegalRepresentativeDocument
            : request.RepresentativeDocument;
        var brandName = string.IsNullOrWhiteSpace(request.BrandName) ? brand.Name : request.BrandName;
        var classes = string.IsNullOrWhiteSpace(request.Classes) ? brand.ClassesSnapshot() : request.Classes;
        var amount = request.Amount > 0 ? request.Amount : tenant.QuotedAmount ?? 0;
        var companyName = tenant.Name;
        var street = tenant.Address?.Street ?? string.Empty;
        var number = tenant.Address?.Number ?? string.Empty;
        var city = tenant.Address?.City ?? string.Empty;
        var state = tenant.Address?.State ?? string.Empty;
        var zipCode = tenant.Address?.ZipCode ?? string.Empty;
        var minimumTerm = ContractTemplateKeys.DefaultMinimumTerm;

        if (string.IsNullOrWhiteSpace(companyDocument))
        {
            throw new ConflictException("Company document is required to pre-issue a contract.");
        }

        if (string.IsNullOrWhiteSpace(representativeDocument))
        {
            throw new ConflictException("Legal representative document is required to pre-issue a contract.");
        }

        var template = templates.GetTemplate(ContractTemplateKeys.PrestacaoServicos);
        var bodyMarkdown = ContractPlaceholders.Merge(
            template,
            ContractPlaceholders.From(
                companyName,
                companyDocument,
                street,
                number,
                city,
                state,
                zipCode,
                brandName!,
                classes ?? string.Empty,
                amount,
                minimumTerm));

        var code = await ContractCode.AllocateAsync(repository, cancellationToken);
        var contract = Contract.PreIssue(
            brand.TenantId,
            brand.Id,
            code,
            companyDocument,
            representativeDocument,
            brandName,
            classes ?? string.Empty,
            amount,
            ContractTemplateKeys.PrestacaoServicos,
            bodyMarkdown,
            companyName,
            street,
            number,
            city,
            state,
            zipCode,
            minimumTerm);

        repository.Add(contract);
        await repository.CommitAsync(cancellationToken);
        return contract.ToDto();
    }
}

public sealed record UpdateContractCommand(
    Guid Id,
    decimal Amount,
    string CompanyDocument,
    string RepresentativeDocument,
    string BrandName,
    string Classes,
    string? BodyMarkdown,
    string? MinimumTerm,
    string? CompanyName,
    string? Street,
    string? Number,
    string? City,
    string? State,
    string? ZipCode) : IRequest<ContractDto>;

public sealed class UpdateContractCommandValidator : AbstractValidator<UpdateContractCommand>
{
    public UpdateContractCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CompanyDocument).NotEmpty().MaximumLength(32);
        RuleFor(x => x.RepresentativeDocument).NotEmpty().MaximumLength(32);
        RuleFor(x => x.BrandName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Classes).MaximumLength(200);
        RuleFor(x => x.BodyMarkdown).MaximumLength(100_000);
        RuleFor(x => x.MinimumTerm).MaximumLength(100);
        RuleFor(x => x.CompanyName).MaximumLength(200);
        RuleFor(x => x.Street).MaximumLength(200);
        RuleFor(x => x.Number).MaximumLength(20);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(2);
        RuleFor(x => x.ZipCode).MaximumLength(16);
    }
}

public sealed class UpdateContractCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<UpdateContractCommand, ContractDto>
{
    public async Task<ContractDto> Handle(UpdateContractCommand request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);

        var contract = await LoadAsync(repository, request.Id, cancellationToken);
        try
        {
            contract.UpdateSnapshot(
                request.CompanyDocument,
                request.RepresentativeDocument,
                request.BrandName,
                request.Classes,
                request.Amount,
                request.CompanyName,
                request.Street,
                request.Number,
                request.City,
                request.State,
                request.ZipCode,
                request.MinimumTerm,
                request.BodyMarkdown);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await repository.CommitAsync(cancellationToken);
        return contract.ToDto();
    }

    internal static async Task<Contract> LoadAsync(IGenericRepository repository, Guid id, CancellationToken cancellationToken) =>
        await repository.FirstOrDefaultAsync<Contract>(c => c.Id == id && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Contract not found.");
}

public sealed record IssueContractCommand(Guid Id) : IRequest<ContractDto>;

public sealed class IssueContractCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext,
    IContractDocumentGenerator documents) : IRequestHandler<IssueContractCommand, ContractDto>
{
    public async Task<ContractDto> Handle(IssueContractCommand request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);
        var contract = await UpdateContractCommandHandler.LoadAsync(repository, request.Id, cancellationToken);
        try
        {
            contract.Issue();
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        var pdf = documents.GeneratePdf(contract.ToDocumentModel(stampFormarcaSignature: true));
        await ContractDocuments.UpsertPdfAsync(repository, contract, ContractFileKind.Issued, pdf, cancellationToken);
        await repository.CommitAsync(cancellationToken);
        return contract.ToDto();
    }
}

public sealed record SignContractCommand(Guid Id, string SignaturePngBase64) : IRequest<ContractDto>;

public sealed class SignContractCommandValidator : AbstractValidator<SignContractCommand>
{
    public SignContractCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SignaturePngBase64).NotEmpty();
    }
}

public sealed class SignContractCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext,
    IContractDocumentGenerator documents) : IRequestHandler<SignContractCommand, ContractDto>
{
    public async Task<ContractDto> Handle(SignContractCommand request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);
        var contract = await UpdateContractCommandHandler.LoadAsync(repository, request.Id, cancellationToken);
        var signature = PngSignature.DecodeRequired(request.SignaturePngBase64);
        try
        {
            contract.SignByClient(signature);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        var pdf = documents.GeneratePdf(contract.ToDocumentModel(stampFormarcaSignature: true));
        await ContractDocuments.UpsertPdfAsync(repository, contract, ContractFileKind.Signed, pdf, cancellationToken);

        var invoiceExists = await repository.AnyAsync<Invoice>(
            i => i.ContractId == contract.Id && !i.IsDeleted, cancellationToken);
        if (!invoiceExists)
        {
            repository.Add(Invoice.CreatePending(contract.TenantId, contract.Id, contract.Amount));
        }

        await repository.CommitAsync(cancellationToken);
        return contract.ToDto();
    }
}

public sealed record CancelContractCommand(Guid Id) : IRequest<ContractDto>;

public sealed class CancelContractCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<CancelContractCommand, ContractDto>
{
    public async Task<ContractDto> Handle(CancelContractCommand request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);
        var contract = await UpdateContractCommandHandler.LoadAsync(repository, request.Id, cancellationToken);
        try
        {
            contract.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await repository.CommitAsync(cancellationToken);
        return contract.ToDto();
    }
}

internal static class ContractCode
{
    public static async Task<string> AllocateAsync(IGenericRepository repository, CancellationToken cancellationToken)
    {
        var year = Clock.Current.UtcNow.Year;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = $"FM-{year}-{Random.Shared.Next(0, 1_000_000):D6}";
            var exists = await repository.AnyAsync<Contract>(
                c => c.Code == code && !c.IsDeleted, cancellationToken);
            if (!exists)
            {
                return code;
            }
        }

        throw new ConflictException("Could not allocate a unique contract code.");
    }
}
