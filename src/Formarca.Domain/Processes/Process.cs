using Formarca.Domain.Common;
using Formarca.Domain.Tenancy;

namespace Formarca.Domain.Processes;

public class Process : Entity, ITenantScoped
{
    private Process()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid BrandId { get; private set; }

    public Guid ContractId { get; private set; }

    public ProcessStatus Status { get; private set; } = ProcessStatus.Generated;

    public string? ProcessNumber { get; private set; }

    public Guid? TrademarkStatusId { get; private set; }

    public DateTime? TrademarkStatusChangedAtUtc { get; private set; }

    public static Process CreateGenerated(Guid tenantId, Guid brandId, Guid contractId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        }

        if (brandId == Guid.Empty)
        {
            throw new ArgumentException("Brand is required.", nameof(brandId));
        }

        if (contractId == Guid.Empty)
        {
            throw new ArgumentException("Contract is required.", nameof(contractId));
        }

        return new Process
        {
            TenantId = tenantId,
            BrandId = brandId,
            ContractId = contractId,
            Status = ProcessStatus.Generated
        };
    }

    public void AssignTrademarkStatus(Guid trademarkStatusId)
    {
        if (trademarkStatusId == Guid.Empty)
        {
            throw new ArgumentException("Status is required.", nameof(trademarkStatusId));
        }

        if (TrademarkStatusId == trademarkStatusId)
        {
            throw new InvalidOperationException("Process already has this status.");
        }

        TrademarkStatusId = trademarkStatusId;
        TrademarkStatusChangedAtUtc = Clock.Current.UtcNow;
        MarkUpdated();
    }

    public void SetProcessNumber(string? processNumber)
    {
        var trimmed = string.IsNullOrWhiteSpace(processNumber) ? null : processNumber.Trim();
        if (trimmed is { Length: > 64 })
        {
            throw new ArgumentException("Process number is too long.", nameof(processNumber));
        }

        ProcessNumber = trimmed;
        MarkUpdated();
    }
}
