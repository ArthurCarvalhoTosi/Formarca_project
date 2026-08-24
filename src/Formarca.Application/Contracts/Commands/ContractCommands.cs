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
using Microsoft.EntityFrameworkCore;

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
    ITenantContext tenantContext) : IRequestHandler<PreIssueContractCommand, ContractDto>
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

        if (string.IsNullOrWhiteSpace(companyDocument))
        {
            throw new ConflictException("Company document is required to pre-issue a contract.");
        }

        if (string.IsNullOrWhiteSpace(representativeDocument))
        {
            throw new ConflictException("Legal representative document is required to pre-issue a contract.");
        }

        var code = await ContractCode.AllocateAsync(repository, cancellationToken);
        var contract = Contract.PreIssue(
            brand.TenantId,
            brand.Id,
            code,
            companyDocument,
            representativeDocument,
            brandName,
            classes ?? string.Empty,
            amount);

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
    string Classes) : IRequest<ContractDto>;

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
                request.Amount);
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
    ITenantContext tenantContext) : IRequestHandler<IssueContractCommand, ContractDto>
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

        await repository.CommitAsync(cancellationToken);
        return contract.ToDto();
    }
}

public sealed record SignContractCommand(Guid Id) : IRequest<ContractDto>;

public sealed class SignContractCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<SignContractCommand, ContractDto>
{
    public async Task<ContractDto> Handle(SignContractCommand request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);
        var contract = await UpdateContractCommandHandler.LoadAsync(repository, request.Id, cancellationToken);
        try
        {
            contract.SignByClient();
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

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
