using Formarca.Domain.Contracts;

namespace Formarca.Domain.Tests;

public class ContractTests
{
    [Fact]
    public void PreIssue_Signs_Formarca_And_Starts_PreIssued()
    {
        var contract = PreIssue();

        Assert.Equal(ContractStatus.PreIssued, contract.Status);
        Assert.Equal("FM-2026-000001", contract.Code);
        Assert.Equal("11222333000181", contract.CompanyDocument);
        Assert.Equal("39053344705", contract.RepresentativeDocument);
        Assert.True(contract.FormarcaSignedAt > DateTime.MinValue);
        Assert.Null(contract.ClientSignedAt);
        Assert.False(contract.CanDownload);
    }

    [Fact]
    public void UpdateSnapshot_Allowed_Only_While_PreIssued()
    {
        var contract = PreIssue();
        contract.UpdateSnapshot("11222333000181", "39053344705", "New Brand", "5", 200m);
        Assert.Equal("New Brand", contract.BrandName);
        Assert.Equal(200m, contract.Amount);

        contract.Issue();
        Assert.Throws<InvalidOperationException>(
            () => contract.UpdateSnapshot("11222333000181", "39053344705", "Nope", "5", 1m));
    }

    [Fact]
    public void Issue_Then_Sign_Creates_Signed_State()
    {
        var contract = PreIssue();
        contract.Issue();
        Assert.Equal(ContractStatus.Issued, contract.Status);
        Assert.True(contract.CanDownload);

        contract.SignByClient();
        Assert.Equal(ContractStatus.Signed, contract.Status);
        Assert.NotNull(contract.ClientSignedAt);
        Assert.NotNull(contract.SignedAtUtc);
    }

    [Fact]
    public void Cancel_Not_Allowed_After_Sign()
    {
        var contract = PreIssue();
        contract.Issue();
        contract.SignByClient();
        Assert.Throws<InvalidOperationException>(contract.Cancel);
    }

    private static Contract PreIssue() =>
        Contract.PreIssue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "FM-2026-000001",
            "11.222.333/0001-81",
            "390.533.447-05",
            "Acme",
            "1, 9",
            100m);
}
