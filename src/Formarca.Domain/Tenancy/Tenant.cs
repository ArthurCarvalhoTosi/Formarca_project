using Formarca.Domain.Common;

namespace Formarca.Domain.Tenancy;

public class Tenant : Entity, IPlatformEntity
{
    private Tenant()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Document { get; private set; }

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

    public void Update(string name, string? document, TenantStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Document = string.IsNullOrWhiteSpace(document) ? null : document.Trim();
        Status = status;
        MarkUpdated();
    }

    public void SetStatus(TenantStatus status)
    {
        Status = status;
        MarkUpdated();
    }
}
