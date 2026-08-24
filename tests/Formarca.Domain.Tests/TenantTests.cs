using Formarca.Domain.Tenancy;

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

    [Fact]
    public void Register_Creates_Pending_Tenant()
    {
        var tenant = RegisterSample();

        Assert.Equal(TenantStatus.Pending, tenant.Status);
        Assert.Equal("acme-ltd", tenant.Slug);
        Assert.Equal("maria@acme.com", tenant.Email);
        Assert.Equal("11987654321", tenant.Phone);
        Assert.Equal("39053344705", tenant.Document);
        Assert.NotNull(tenant.Address);
        Assert.Equal("SP", tenant.Address.State);
        Assert.Equal("01310100", tenant.Address.ZipCode);
    }

    [Fact]
    public void Approve_Sets_Onboarding()
    {
        var tenant = RegisterSample();
        tenant.Approve();
        Assert.Equal(TenantStatus.Onboarding, tenant.Status);
    }

    [Fact]
    public void ActivateAfterPayment_Sets_Active()
    {
        var tenant = RegisterSample();
        tenant.Approve();
        tenant.ActivateAfterPayment();
        Assert.Equal(TenantStatus.Active, tenant.Status);
    }

    [Fact]
    public void ActivateAfterPayment_Throws_When_Already_Active()
    {
        var tenant = RegisterSample();
        tenant.Approve();
        tenant.ActivateAfterPayment();
        Assert.Throws<InvalidOperationException>(tenant.ActivateAfterPayment);
    }

    [Fact]
    public void Reject_Sets_Inactive()
    {
        var tenant = RegisterSample();
        tenant.Reject();
        Assert.Equal(TenantStatus.Inactive, tenant.Status);
    }

    [Fact]
    public void Approve_Throws_When_Not_Pending()
    {
        var tenant = Tenant.Create("Acme", "acme");
        Assert.Throws<InvalidOperationException>(tenant.Approve);
    }

    [Fact]
    public void UpdateRegistration_Replaces_Contact_And_Commercial_Fields()
    {
        var tenant = RegisterSample();
        tenant.Approve();

        tenant.UpdateRegistration(
            "New Name",
            "new@acme.com",
            "1134567890",
            "11222333000181",
            Address.Create("01310-100", "Av Paulista", "1000", "cj 10", "Bela Vista", "São Paulo", "sp"),
            "Maria Souza",
            "390.533.447-05",
            2500m,
            "Notes");

        Assert.Equal("New Name", tenant.Name);
        Assert.Equal("new@acme.com", tenant.Email);
        Assert.Equal("1134567890", tenant.Phone);
        Assert.Equal("11222333000181", tenant.Document);
        Assert.Equal("Maria Souza", tenant.LegalRepresentativeName);
        Assert.Equal("39053344705", tenant.LegalRepresentativeDocument);
        Assert.Equal(2500m, tenant.QuotedAmount);
        Assert.Equal("Notes", tenant.BudgetNotes);
        Assert.Equal("SP", tenant.Address!.State);
        Assert.Equal(TenantStatus.Onboarding, tenant.Status);
        Assert.Equal("acme-ltd", tenant.Slug);
    }

    internal static Tenant RegisterSample() =>
        Tenant.Register(
            "Acme Ltd",
            "acme-ltd",
            "Maria@Acme.com",
            "(11) 98765-4321",
            "390.533.447-05",
            Address.Create("01310-100", "Av Paulista", "100", null, "Bela Vista", "São Paulo", "SP"));
}
