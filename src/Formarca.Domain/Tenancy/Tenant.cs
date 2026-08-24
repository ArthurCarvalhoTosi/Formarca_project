using Formarca.Domain.Common;

namespace Formarca.Domain.Tenancy;

public class Tenant : Entity, IPlatformEntity
{
    private Tenant()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? Document { get; private set; }

    public string? LegalRepresentativeName { get; private set; }

    public string? LegalRepresentativeDocument { get; private set; }

    public decimal? QuotedAmount { get; private set; }

    public string? BudgetNotes { get; private set; }

    public Address? Address { get; private set; }

    public TenantStatus Status { get; private set; } = TenantStatus.Active;

    public static Tenant Create(string name, string slug, string? document = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return new Tenant
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Document = string.IsNullOrWhiteSpace(document) ? null : document.Trim(),
            Status = TenantStatus.Active
        };
    }

    public static Tenant Register(
        string name,
        string slug,
        string email,
        string phone,
        string document,
        Address address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(document);
        ArgumentNullException.ThrowIfNull(address);

        return new Tenant
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = BrazilianGeographic.NormalizePhone(phone),
            Document = BrazilianDocuments.DigitsOnly(document),
            Address = address,
            Status = TenantStatus.Pending
        };
    }

    public void Update(string name, string? document, TenantStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Document = string.IsNullOrWhiteSpace(document) ? null : document.Trim();
        Status = status;
        MarkUpdated();
    }

    public void UpdateRegistration(
        string name,
        string email,
        string phone,
        string document,
        Address address,
        string? legalRepresentativeName,
        string? legalRepresentativeDocument,
        decimal? quotedAmount,
        string? budgetNotes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(document);
        ArgumentNullException.ThrowIfNull(address);

        if (quotedAmount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quotedAmount), "Quoted amount cannot be negative.");
        }

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = BrazilianGeographic.NormalizePhone(phone);
        Document = BrazilianDocuments.DigitsOnly(document);
        Address = address;
        LegalRepresentativeName = string.IsNullOrWhiteSpace(legalRepresentativeName)
            ? null
            : legalRepresentativeName.Trim();
        LegalRepresentativeDocument = string.IsNullOrWhiteSpace(legalRepresentativeDocument)
            ? null
            : BrazilianDocuments.DigitsOnly(legalRepresentativeDocument);
        QuotedAmount = quotedAmount;
        BudgetNotes = string.IsNullOrWhiteSpace(budgetNotes) ? null : budgetNotes.Trim();
        MarkUpdated();
    }

    public void Approve()
    {
        if (Status != TenantStatus.Pending)
        {
            throw new InvalidOperationException("Only pending tenants can be approved.");
        }

        Status = TenantStatus.Onboarding;
        MarkUpdated();
    }

    public void ActivateAfterPayment()
    {
        if (Status != TenantStatus.Onboarding)
        {
            throw new InvalidOperationException("Only onboarding tenants can be activated after payment.");
        }

        Status = TenantStatus.Active;
        MarkUpdated();
    }

    public void Reject()
    {
        if (Status != TenantStatus.Pending)
        {
            throw new InvalidOperationException("Only pending tenants can be rejected.");
        }

        Status = TenantStatus.Inactive;
        MarkUpdated();
    }

    public void SetStatus(TenantStatus status)
    {
        Status = status;
        MarkUpdated();
    }
}
