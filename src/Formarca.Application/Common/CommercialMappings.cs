using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.Brands;
using Formarca.Domain.Contracts;
using Formarca.Domain.Invoices;
using Formarca.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using ProcessEntity = Formarca.Domain.Processes.Process;

namespace Formarca.Application.Common;

internal static class CommercialMappings
{
    public static BrandDto ToDto(this Brand brand) =>
        new(
            brand.Id,
            brand.TenantId,
            brand.Name,
            brand.ServicesDescription,
            brand.InpiClasses.Select(c => c.Number).OrderBy(n => n).ToList(),
            brand.CreatedAtUtc);

    public static ContractDto ToDto(this Contract contract, string? tenantName = null, Invoice? invoice = null) =>
        new(
            contract.Id,
            contract.TenantId,
            contract.BrandId,
            contract.Code,
            contract.CompanyDocument,
            contract.RepresentativeDocument,
            contract.BrandName,
            contract.Classes,
            contract.Amount,
            contract.Status.ToString(),
            contract.FormarcaSignedAt,
            contract.ClientSignedAt,
            contract.GeneratedAtUtc,
            contract.SignedAtUtc,
            contract.CreatedAtUtc,
            contract.TemplateKey,
            contract.BodyMarkdown,
            contract.CompanyName,
            contract.Street,
            contract.Number,
            contract.City,
            contract.State,
            contract.ZipCode,
            contract.MinimumTerm,
            contract.ClientSignaturePng,
            tenantName,
            invoice is { Status: InvoiceStatus.Paid } ? invoice.ReceivedAmount : null,
            invoice?.PaymentMethod?.ToString(),
            invoice?.Id);

    public static InvoiceDto ToDto(this Invoice invoice, string? tenantName = null, string? contractCode = null) =>
        new(
            invoice.Id,
            invoice.TenantId,
            invoice.ContractId,
            invoice.PaymentMethod?.ToString(),
            invoice.Installments,
            invoice.ContractAmount,
            invoice.Discount,
            invoice.ExpectedAmount,
            invoice.ReceivedAmount,
            invoice.Status.ToString(),
            invoice.CreatedAtUtc,
            tenantName,
            contractCode);

    public static InvoiceSettingsDto ToDto(this InvoiceSettings settings)
    {
        var rates = settings.GetInterestRates();
        var interestRates = new Dictionary<string, decimal>(13);
        for (var i = 1; i <= 12; i++)
        {
            interestRates[i.ToString()] = rates.GetInstallmentRate(i);
        }

        interestRates[InterestRates.CardCashKey] = rates.CardCash;
        return new InvoiceSettingsDto(
            settings.Id,
            interestRates,
            settings.InterestRatesJson,
            settings.PixDiscount,
            settings.GeneralDiscount);
    }

    public static ProcessDto ToDto(
        this ProcessEntity process,
        string? brandName = null,
        string? contractCode = null) =>
        new(
            process.Id,
            process.TenantId,
            process.BrandId,
            process.ContractId,
            process.Status.ToString(),
            process.ProcessNumber,
            process.CreatedAtUtc,
            brandName,
            contractCode);

    public static async Task<IReadOnlyList<ContractDto>> MapAsync(
        IGenericRepository repository,
        IReadOnlyList<Contract> items,
        CancellationToken cancellationToken)
    {
        var tenantIds = items.Select(c => c.TenantId).Distinct().ToList();
        var tenantNames = tenantIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await repository.Query<Tenant>()
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        var contractIds = items.Select(c => c.Id).ToList();
        var invoices = contractIds.Count == 0
            ? new List<Invoice>()
            : await repository.Query<Invoice>()
                .Where(i => contractIds.Contains(i.ContractId) && !i.IsDeleted)
                .ToListAsync(cancellationToken);
        var invoiceByContract = invoices
            .GroupBy(i => i.ContractId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.CreatedAtUtc).First());

        return items.Select(contract =>
        {
            tenantNames.TryGetValue(contract.TenantId, out var tenantName);
            invoiceByContract.TryGetValue(contract.Id, out var invoice);
            return contract.ToDto(tenantName, invoice);
        }).ToList();
    }

    public static async Task<IReadOnlyList<InvoiceDto>> MapAsync(
        IGenericRepository repository,
        IReadOnlyList<Invoice> items,
        CancellationToken cancellationToken)
    {
        var tenantIds = items.Select(i => i.TenantId).Distinct().ToList();
        var tenantNames = tenantIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await repository.Query<Tenant>()
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        var contractIds = items.Select(i => i.ContractId).Distinct().ToList();
        var contractCodes = contractIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await repository.Query<Contract>()
                .Where(c => contractIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Code, cancellationToken);

        return items.Select(invoice =>
        {
            tenantNames.TryGetValue(invoice.TenantId, out var tenantName);
            contractCodes.TryGetValue(invoice.ContractId, out var contractCode);
            return invoice.ToDto(tenantName, contractCode);
        }).ToList();
    }

    public static async Task<IReadOnlyList<ProcessDto>> MapAsync(
        IGenericRepository repository,
        IReadOnlyList<ProcessEntity> items,
        CancellationToken cancellationToken)
    {
        var brandIds = items.Select(p => p.BrandId).Distinct().ToList();
        var brandNames = brandIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await repository.Query<Brand>()
                .Where(b => brandIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Name, cancellationToken);

        var contractIds = items.Select(p => p.ContractId).Distinct().ToList();
        var contractCodes = contractIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await repository.Query<Contract>()
                .Where(c => contractIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Code, cancellationToken);

        return items.Select(process =>
        {
            brandNames.TryGetValue(process.BrandId, out var brandName);
            contractCodes.TryGetValue(process.ContractId, out var contractCode);
            return process.ToDto(brandName, contractCode);
        }).ToList();
    }
}
