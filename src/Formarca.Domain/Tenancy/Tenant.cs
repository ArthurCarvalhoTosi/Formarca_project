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

    public string? BrandName { get; private set; }

    public string? BrandDescription { get; private set; }

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
        Address address,
        string brandName,
        string brandDescription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(document);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(brandName);
        ArgumentException.ThrowIfNullOrWhiteSpace(brandDescription);

        return new Tenant
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = BrazilianGeographic.NormalizePhone(phone),
            Document = BrazilianDocuments.DigitsOnly(document),
            Address = address,
            BrandName = brandName.Trim(),
            BrandDescription = brandDescription.Trim(),
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
        string brandName,
        string brandDescription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(document);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(brandName);
        ArgumentException.ThrowIfNullOrWhiteSpace(brandDescription);

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = BrazilianGeographic.NormalizePhone(phone);
        Document = BrazilianDocuments.DigitsOnly(document);
        Address = address;
        BrandName = brandName.Trim();
        BrandDescription = brandDescription.Trim();
        MarkUpdated();
    }

    public void Approve()
    {
        if (Status != TenantStatus.Pending)
        {
            throw new InvalidOperationException("Only pending tenants can be approved.");
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
