using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.Brands;
using Formarca.Domain.Contracts;
using Formarca.Domain.Invoices;
using Formarca.Domain.Processes;
using Formarca.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using ProcessEntity = Formarca.Domain.Processes.Process;

namespace Formarca.Application.Common;

internal static class CommercialMappings
{
    public static BrandDto ToDto(this Brand brand, string? tenantName = null) =>
        new(
            brand.Id,
            brand.TenantId,
            brand.Name,
            brand.ServicesDescription,
            brand.InpiClasses.Select(c => c.Number).OrderBy(n => n).ToList(),
            brand.CreatedAtUtc,
            tenantName);

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
            invoice?.Id,
            contract.AutentiqueDocumentId,
            contract.AutentiqueSignUrl,
            contract.AutentiqueStatus);

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
        string? contractCode = null,
        TrademarkStatus? trademarkStatus = null) =>
        new(
            process.Id,
            process.TenantId,
            process.BrandId,
            process.ContractId,
            process.Status.ToString(),
            process.ProcessNumber,
            process.CreatedAtUtc,
            brandName,
            contractCode,
            process.TrademarkStatusId,
            trademarkStatus?.Title,
            trademarkStatus?.Tone.ToString(),
            process.TrademarkStatusChangedAtUtc);

    public static async Task<IReadOnlyList<BrandDto>> MapAsync(
        IGenericRepository repository,
        IReadOnlyList<Brand> items,
        CancellationToken cancellationToken)
    {
        var tenantIds = items.Select(b => b.TenantId).Distinct().ToList();
        var tenantNames = tenantIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await repository.Query<Tenant>()
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        return items.Select(brand =>
        {
            tenantNames.TryGetValue(brand.TenantId, out var tenantName);
            return brand.ToDto(tenantName);
        }).ToList();
    }

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

        var statusIds = items
            .Where(p => p.TrademarkStatusId.HasValue)
            .Select(p => p.TrademarkStatusId!.Value)
            .Distinct()
            .ToList();
        var trademarkStatuses = statusIds.Count == 0
            ? new Dictionary<Guid, TrademarkStatus>()
            : await repository.Query<TrademarkStatus>()
                .Where(s => statusIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, cancellationToken);

        return items.Select(process =>
        {
            brandNames.TryGetValue(process.BrandId, out var brandName);
            contractCodes.TryGetValue(process.ContractId, out var contractCode);
            TrademarkStatus? trademarkStatus = null;
            if (process.TrademarkStatusId is Guid statusId)
            {
                trademarkStatuses.TryGetValue(statusId, out trademarkStatus);
            }

            return process.ToDto(brandName, contractCode, trademarkStatus);
        }).ToList();
    }

    public static ProcessFunnelStage ResolveFunnelStage(
        Contract? contract,
        Invoice? invoice,
        ProcessEntity? process)
    {
        if (process is not null)
        {
            return ProcessFunnelStage.Process;
        }

        if (invoice is not null && invoice.Status != InvoiceStatus.Paid)
        {
            return ProcessFunnelStage.Invoice;
        }

        if (contract is not null)
        {
            return ProcessFunnelStage.Contract;
        }

        return ProcessFunnelStage.Brand;
    }

    public static ProcessDashboardItemDto ToDashboardItem(
        this Brand brand,
        string? tenantName,
        Contract? contract,
        Invoice? invoice,
        ProcessEntity? process)
    {
        return new ProcessDashboardItemDto(
            brand.Id,
            brand.Name,
            brand.TenantId,
            tenantName,
            contract is null
                ? null
                : new ProcessDashboardContractDto(contract.Id, contract.Code, contract.Status.ToString()),
            invoice is null
                ? null
                : new ProcessDashboardInvoiceDto(invoice.Id, invoice.Status.ToString(), invoice.ExpectedAmount),
            process is null
                ? null
                : new ProcessDashboardProcessDto(process.Id, process.Status.ToString(), process.ProcessNumber),
            ResolveFunnelStage(contract, invoice, process));
    }

    public static ProcessDashboardKpisDto ToDashboardKpis(IReadOnlyList<ProcessDashboardItemDto> items)
    {
        var contractsInProgress = 0;
        var invoicesPending = 0;
        var processesGenerated = 0;

        foreach (var item in items)
        {
            if (item.Contract is not null
                && item.Process is null
                && item.Invoice is not { Status: nameof(InvoiceStatus.Paid) })
            {
                contractsInProgress++;
            }

            if (item.Invoice is { Status: nameof(InvoiceStatus.PendingPayment) or nameof(InvoiceStatus.AwaitingApproval) })
            {
                invoicesPending++;
            }

            if (item.Process is not null)
            {
                processesGenerated++;
            }
        }

        return new ProcessDashboardKpisDto(items.Count, contractsInProgress, invoicesPending, processesGenerated);
    }

    public static async Task<IReadOnlyList<ProcessDashboardItemDto>> MapDashboardAsync(
        IGenericRepository repository,
        IReadOnlyList<Brand> brands,
        CancellationToken cancellationToken)
    {
        if (brands.Count == 0)
        {
            return [];
        }

        var tenantIds = brands.Select(b => b.TenantId).Distinct().ToList();
        var tenantNames = await repository.Query<Tenant>()
            .Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        var brandIds = brands.Select(b => b.Id).ToList();
        var contracts = await repository.Query<Contract>()
            .Where(c => brandIds.Contains(c.BrandId) && !c.IsDeleted)
            .ToListAsync(cancellationToken);
        var latestContractByBrand = contracts
            .GroupBy(c => c.BrandId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(c => c.CreatedAtUtc).ThenByDescending(c => c.Id).First());

        var contractIds = latestContractByBrand.Values.Select(c => c.Id).ToList();
        var invoices = contractIds.Count == 0
            ? new List<Invoice>()
            : await repository.Query<Invoice>()
                .Where(i => contractIds.Contains(i.ContractId) && !i.IsDeleted)
                .ToListAsync(cancellationToken);
        var latestInvoiceByContract = invoices
            .GroupBy(i => i.ContractId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(i => i.CreatedAtUtc).ThenByDescending(i => i.Id).First());

        var processes = contractIds.Count == 0
            ? new List<ProcessEntity>()
            : await repository.Query<ProcessEntity>()
                .Where(p => contractIds.Contains(p.ContractId) && !p.IsDeleted)
                .ToListAsync(cancellationToken);
        var processByContract = processes
            .GroupBy(p => p.ContractId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(p => p.CreatedAtUtc).ThenByDescending(p => p.Id).First());

        return brands.Select(brand =>
        {
            tenantNames.TryGetValue(brand.TenantId, out var tenantName);
            latestContractByBrand.TryGetValue(brand.Id, out var contract);
            Invoice? invoice = null;
            ProcessEntity? process = null;
            if (contract is not null)
            {
                latestInvoiceByContract.TryGetValue(contract.Id, out invoice);
                processByContract.TryGetValue(contract.Id, out process);
            }

            return brand.ToDashboardItem(tenantName, contract, invoice, process);
        }).ToList();
    }
}
