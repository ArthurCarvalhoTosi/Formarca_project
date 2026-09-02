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
        Assert.Equal("Acme Ltd", contract.CompanyName);
        Assert.Equal("Av Paulista", contract.Street);
        Assert.Equal("100", contract.Number);
        Assert.Equal("São Paulo", contract.City);
        Assert.Equal("SP", contract.State);
        Assert.Equal("01310100", contract.ZipCode);
        Assert.Equal(ContractTemplateKeys.DefaultMinimumTerm, contract.MinimumTerm);
        Assert.Equal(ContractTemplateKeys.PrestacaoServicos, contract.TemplateKey);
        Assert.Contains("Acme Ltd", contract.BodyMarkdown);
        Assert.True(contract.FormarcaSignedAt > DateTime.MinValue);
        Assert.Null(contract.ClientSignedAt);
        Assert.False(contract.CanDownload);
        Assert.True(contract.CanPreview);
        Assert.True(contract.HasCompleteIssueFields);
    }

    [Fact]
    public void UpdateSnapshot_Allowed_Only_While_PreIssued()
    {
        var contract = PreIssue();
        contract.UpdateSnapshot(
            "11222333000181",
            "39053344705",
            "New Brand",
            "5",
            200m,
            "New Co",
            "Rua A",
            "10",
            "Porto Alegre",
            "rs",
            "90010-000",
            "24 (vinte e quatro) meses",
            "# Novo corpo");
        Assert.Equal("New Brand", contract.BrandName);
        Assert.Equal(200m, contract.Amount);
        Assert.Equal("New Co", contract.CompanyName);
        Assert.Equal("RS", contract.State);
        Assert.Equal("90010000", contract.ZipCode);
        Assert.Equal("24 (vinte e quatro) meses", contract.MinimumTerm);
        Assert.Equal("# Novo corpo", contract.BodyMarkdown);

        contract.Issue();
        Assert.Throws<InvalidOperationException>(
            () => contract.UpdateSnapshot("11222333000181", "39053344705", "Nope", "5", 1m));
    }

    [Fact]
    public void Issue_Blocked_When_Address_Incomplete()
    {
        var contract = Contract.PreIssue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "FM-2026-000009",
            "11.222.333/0001-81",
            "390.533.447-05",
            "Acme",
            "1, 9",
            100m,
            bodyMarkdown: "# Contrato");

        var ex = Assert.Throws<InvalidOperationException>(contract.Issue);
        Assert.Contains("cannot be issued", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ContractStatus.PreIssued, contract.Status);
    }

    [Fact]
    public void Issue_Then_Sign_Creates_Signed_State()
    {
        var contract = PreIssue();
        contract.Issue();
        Assert.Equal(ContractStatus.Issued, contract.Status);
        Assert.True(contract.CanDownload);
        Assert.Equal(ContractFileKind.Issued, contract.PersistedFileKind);

        var png = "png"u8.ToArray();
        contract.SignByClient(png);
        Assert.Equal(ContractStatus.Signed, contract.Status);
        Assert.NotNull(contract.ClientSignedAt);
        Assert.NotNull(contract.SignedAtUtc);
        Assert.Equal(png, contract.ClientSignaturePng);
        Assert.Equal(ContractFileKind.Signed, contract.PersistedFileKind);
    }

    [Fact]
    public void Cancel_Not_Allowed_After_Sign()
    {
        var contract = PreIssue();
        contract.Issue();
        contract.SignByClient();
        Assert.Throws<InvalidOperationException>(contract.Cancel);
    }

    [Fact]
    public void ContractFile_Stores_Pdf_Bytes()
    {
        var tenantId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var pdf = "%PDF-1.7"u8.ToArray();
        var file = ContractFile.CreatePdf(tenantId, contractId, ContractFileKind.Issued, "FM-2026-000001.pdf", pdf);

        Assert.Equal(tenantId, file.TenantId);
        Assert.Equal(contractId, file.ContractId);
        Assert.Equal(ContractFileKind.Issued, file.Kind);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal(pdf, file.Content);

        var signed = "%PDF-signed"u8.ToArray();
        file.ReplaceContent(signed);
        Assert.Equal(signed, file.Content);
        Assert.Throws<ArgumentException>(() => file.ReplaceContent([]));
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
            100m,
            ContractTemplateKeys.PrestacaoServicos,
            "# Contrato\n\n**CONTRATANTE:** Acme Ltd",
            "Acme Ltd",
            "Av Paulista",
            "100",
            "São Paulo",
            "SP",
            "01310-100",
            ContractTemplateKeys.DefaultMinimumTerm);
}
