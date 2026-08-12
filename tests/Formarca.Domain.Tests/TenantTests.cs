using Formarca.Domain.Tenancy;
using Xunit;

namespace Formarca.Domain.Tests;

public class TenantTests
{
    [Fact]
    public void Create_Normalizes_Slug()
    {
        var tenant = Tenant.Create("Acme Ltd", "Acme-Ltd", "123");

        Assert.Equal("acme-ltd", tenant.Slug);
        Assert.Equal("Acme Ltd", tenant.Name);
        Assert.Equal(TenantStatus.Active, tenant.Status);
    }
}
