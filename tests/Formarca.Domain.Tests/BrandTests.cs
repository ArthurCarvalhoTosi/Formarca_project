using Formarca.Domain.Brands;

namespace Formarca.Domain.Tests;

public class BrandTests
{
    [Fact]
    public void Create_Stores_Classes()
    {
        var tenantId = Guid.NewGuid();
        var brand = Brand.Create(tenantId, "Acme", "Widgets", [45, 1, 1, 9]);

        Assert.Equal(tenantId, brand.TenantId);
        Assert.Equal("Acme", brand.Name);
        Assert.Equal([1, 9, 45], brand.InpiClasses.Select(c => c.Number));
        Assert.Equal("1, 9, 45", brand.ClassesSnapshot());
    }

    [Fact]
    public void Create_Rejects_Invalid_Class()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Brand.Create(Guid.NewGuid(), "Acme", "Widgets", [46]));
    }
}
