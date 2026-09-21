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

    public string TemplateKey { get; private set; } = ContractTemplateKeys.PrestacaoServicos;

    public string BodyMarkdown { get; private set; } = string.Empty;

    public string CompanyName { get; private set; } = string.Empty;

    public string Street { get; private set; } = string.Empty;

    public string Number { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string State { get; private set; } = string.Empty;

    public string ZipCode { get; private set; } = string.Empty;

    public string MinimumTerm { get; private set; } = ContractTemplateKeys.DefaultMinimumTerm;

    public byte[]? ClientSignaturePng { get; private set; }

    public string? AutentiqueDocumentId { get; private set; }

    public string? AutentiqueSignerPublicId { get; private set; }

    public string? AutentiqueSignUrl { get; private set; }

    public string? AutentiqueStatus { get; private set; }

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
        decimal amount,
        string? templateKey = null,
        string? bodyMarkdown = null,
        string? companyName = null,
        string? street = null,
        string? number = null,
        string? city = null,
        string? state = null,
        string? zipCode = null,
        string? minimumTerm = null)
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
            TemplateKey = NormalizeTemplateKey(templateKey),
            BodyMarkdown = bodyMarkdown ?? string.Empty,
            CompanyName = TrimOrEmpty(companyName),
            Street = TrimOrEmpty(street),
            Number = TrimOrEmpty(number),
            City = TrimOrEmpty(city),
            State = NormalizeState(state),
            ZipCode = BrazilianDocuments.DigitsOnly(zipCode),
            MinimumTerm = string.IsNullOrWhiteSpace(minimumTerm)
                ? ContractTemplateKeys.DefaultMinimumTerm
                : minimumTerm.Trim(),
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
        decimal amount,
        string? companyName = null,
        string? street = null,
        string? number = null,
        string? city = null,
        string? state = null,
        string? zipCode = null,
        string? minimumTerm = null,
        string? bodyMarkdown = null)
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
        if (companyName is not null)
        {
            CompanyName = TrimOrEmpty(companyName);
        }

        if (street is not null)
        {
            Street = TrimOrEmpty(street);
        }

        if (number is not null)
        {
            Number = TrimOrEmpty(number);
        }

        if (city is not null)
        {
            City = TrimOrEmpty(city);
        }

        if (state is not null)
        {
            State = NormalizeState(state);
        }

        if (zipCode is not null)
        {
            ZipCode = BrazilianDocuments.DigitsOnly(zipCode);
        }

        if (minimumTerm is not null)
        {
            MinimumTerm = string.IsNullOrWhiteSpace(minimumTerm)
                ? ContractTemplateKeys.DefaultMinimumTerm
                : minimumTerm.Trim();
        }

        if (bodyMarkdown is not null)
        {
            BodyMarkdown = bodyMarkdown;
        }

        MarkUpdated();
    }

    public void Issue()
    {
        EnsurePreIssued();
        EnsureReadyToIssue();
        Status = ContractStatus.Issued;
        MarkUpdated();
    }

    public void SignByClient(byte[]? signaturePng = null)
    {
        if (Status != ContractStatus.Issued)
        {
            throw new InvalidOperationException("Only issued contracts can be signed.");
        }

        if (signaturePng is { Length: > 0 })
        {
            ClientSignaturePng = signaturePng;
        }

        var now = Clock.Current.UtcNow;
        ClientSignedAt = now;
        SignedAtUtc = now;
        Status = ContractStatus.Signed;
        if (!string.IsNullOrWhiteSpace(AutentiqueDocumentId)
            && AutentiqueStatus != AutentiqueDocumentStatus.Rejected)
        {
            AutentiqueStatus = AutentiqueDocumentStatus.Finished;
        }

        MarkUpdated();
    }

    public void AttachAutentiqueSigning(string documentId, string? signerPublicId, string? signUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        AutentiqueDocumentId = documentId.Trim();
        AutentiqueSignerPublicId = TrimToNull(signerPublicId);
        AutentiqueSignUrl = TrimToNull(signUrl);
        AutentiqueStatus = AutentiqueDocumentStatus.Pending;
        MarkUpdated();
    }

    public void SetAutentiqueSignUrl(string signUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signUrl);
        AutentiqueSignUrl = signUrl.Trim();
        MarkUpdated();
    }

    public void MarkAutentiqueRejected()
    {
        AutentiqueStatus = AutentiqueDocumentStatus.Rejected;
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

    public bool CanPreview => Status is ContractStatus.PreIssued or ContractStatus.Issued or ContractStatus.Signed;

    public bool HasCompleteIssueFields =>
        !string.IsNullOrWhiteSpace(CompanyName)
        && !string.IsNullOrWhiteSpace(Street)
        && !string.IsNullOrWhiteSpace(Number)
        && !string.IsNullOrWhiteSpace(City)
        && !string.IsNullOrWhiteSpace(State)
        && !string.IsNullOrWhiteSpace(ZipCode)
        && !string.IsNullOrWhiteSpace(CompanyDocument)
        && !string.IsNullOrWhiteSpace(BrandName)
        && !string.IsNullOrWhiteSpace(BodyMarkdown)
        && !string.IsNullOrWhiteSpace(MinimumTerm);

    public ContractFileKind PersistedFileKind => Status switch
    {
        ContractStatus.Signed => ContractFileKind.Signed,
        _ => ContractFileKind.Issued
    };

    private void EnsurePreIssued()
    {
        if (Status != ContractStatus.PreIssued)
        {
            throw new InvalidOperationException("Only pre-issued contracts can be edited.");
        }
    }

    private void EnsureReadyToIssue()
    {
        if (!HasCompleteIssueFields)
        {
            throw new InvalidOperationException(
                "Contract cannot be issued until company name, address, period, and body are complete.");
        }
    }

    private static string NormalizeTemplateKey(string? templateKey) =>
        string.IsNullOrWhiteSpace(templateKey)
            ? ContractTemplateKeys.PrestacaoServicos
            : templateKey.Trim().ToLowerInvariant();

    private static string TrimOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeState(string? state) =>
        string.IsNullOrWhiteSpace(state) ? string.Empty : state.Trim().ToUpperInvariant();
}
