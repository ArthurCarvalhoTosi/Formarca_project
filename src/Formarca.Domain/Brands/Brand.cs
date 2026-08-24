using Formarca.Domain.Common;
using Formarca.Domain.Tenancy;

namespace Formarca.Domain.Brands;

public class Brand : Entity, ITenantScoped
{
    private readonly List<BrandNiceClass> _inpiClasses = [];

    private Brand()
    {
    }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string ServicesDescription { get; private set; } = string.Empty;

    public IReadOnlyCollection<BrandNiceClass> InpiClasses => _inpiClasses.AsReadOnly();

    public static Brand Create(
        Guid tenantId,
        string name,
        string servicesDescription,
        IEnumerable<int>? inpiClasses = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(servicesDescription);

        var brand = new Brand
        {
            TenantId = tenantId,
            Name = name.Trim(),
            ServicesDescription = servicesDescription.Trim()
        };
        brand.ReplaceClasses(inpiClasses);
        return brand;
    }

    public void Update(string name, string servicesDescription, IEnumerable<int>? inpiClasses)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(servicesDescription);

        Name = name.Trim();
        ServicesDescription = servicesDescription.Trim();
        ReplaceClasses(inpiClasses);
        MarkUpdated();
    }

    public string ClassesSnapshot() =>
        string.Join(", ", _inpiClasses.Select(c => c.Number).OrderBy(n => n));

    private void ReplaceClasses(IEnumerable<int>? inpiClasses)
    {
        _inpiClasses.Clear();
        if (inpiClasses is null)
        {
            return;
        }

        foreach (var number in inpiClasses.Distinct().OrderBy(n => n))
        {
            _inpiClasses.Add(BrandNiceClass.Create(number));
        }
    }
}
