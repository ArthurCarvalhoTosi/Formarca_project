using Formarca.Domain.Common;
using Formarca.Domain.Tenancy;

namespace Formarca.Domain.Contracts;

public class Contract : Entity, ITenantScoped
{
    private Contract()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid BrandId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string CompanyDocument { get; private set; } = string.Empty;

    public string RepresentativeDocument { get; private set; } = string.Empty;

    public string BrandName { get; private set; } = string.Empty;

    public string Classes { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public ContractStatus Status { get; private set; } = ContractStatus.PreIssued;

    public DateTime FormarcaSignedAt { get; private set; }

    public DateTime? ClientSignedAt { get; private set; }

    public DateTime GeneratedAtUtc { get; private set; }

    public DateTime? SignedAtUtc { get; private set; }

    public static Contract PreIssue(
        Guid tenantId,
        Guid brandId,
        string code,
        string companyDocument,
        string representativeDocument,
        string brandName,
        string classes,
        decimal amount)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        }

        if (brandId == Guid.Empty)
        {
            throw new ArgumentException("Brand is required.", nameof(brandId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(companyDocument);
        ArgumentException.ThrowIfNullOrWhiteSpace(representativeDocument);
        ArgumentException.ThrowIfNullOrWhiteSpace(brandName);

        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        }

        var now = Clock.Current.UtcNow;
        return new Contract
        {
            TenantId = tenantId,
            BrandId = brandId,
            Code = code.Trim().ToUpperInvariant(),
            CompanyDocument = BrazilianDocuments.DigitsOnly(companyDocument),
            RepresentativeDocument = BrazilianDocuments.DigitsOnly(representativeDocument),
            BrandName = brandName.Trim(),
            Classes = (classes ?? string.Empty).Trim(),
            Amount = amount,
            Status = ContractStatus.PreIssued,
            FormarcaSignedAt = now,
            GeneratedAtUtc = now
        };
    }

    public void UpdateSnapshot(
        string companyDocument,
        string representativeDocument,
        string brandName,
        string classes,
        decimal amount)
    {
        EnsurePreIssued();
        ArgumentException.ThrowIfNullOrWhiteSpace(companyDocument);
        ArgumentException.ThrowIfNullOrWhiteSpace(representativeDocument);
        ArgumentException.ThrowIfNullOrWhiteSpace(brandName);

        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        }

        CompanyDocument = BrazilianDocuments.DigitsOnly(companyDocument);
        RepresentativeDocument = BrazilianDocuments.DigitsOnly(representativeDocument);
        BrandName = brandName.Trim();
        Classes = (classes ?? string.Empty).Trim();
        Amount = amount;
        MarkUpdated();
    }

    public void Issue()
    {
        EnsurePreIssued();
        Status = ContractStatus.Issued;
        MarkUpdated();
    }

    public void SignByClient()
    {
        if (Status != ContractStatus.Issued)
        {
            throw new InvalidOperationException("Only issued contracts can be signed.");
        }

        var now = Clock.Current.UtcNow;
        ClientSignedAt = now;
        SignedAtUtc = now;
        Status = ContractStatus.Signed;
        MarkUpdated();
    }

    public void Cancel()
    {
        if (Status is ContractStatus.Signed or ContractStatus.Cancelled)
        {
            throw new InvalidOperationException("Signed or cancelled contracts cannot be cancelled.");
        }

        Status = ContractStatus.Cancelled;
        MarkUpdated();
    }

    public bool CanDownload => Status is ContractStatus.Issued or ContractStatus.Signed;

    private void EnsurePreIssued()
    {
        if (Status != ContractStatus.PreIssued)
        {
            throw new InvalidOperationException("Only pre-issued contracts can be edited.");
        }
    }
}
