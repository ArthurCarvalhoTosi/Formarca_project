using Formarca.Application.Common.Exceptions;
using Formarca.Application.Contracts;
using Formarca.Application.Contracts.Commands;
using Formarca.Domain.Brands;
using Formarca.Domain.Contracts;
using Formarca.Domain.Invoices;
using Formarca.Domain.Tenancy;

namespace Formarca.Application.Tests;

public class ContractCommandTests
{
    private const string TinyPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=";

    [Fact]
    public async Task PreIssue_Clones_Template_And_Snapshots_Tenant_Address()
    {
        var fixture = Seed();
        var handler = new PreIssueContractCommandHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            fixture.Repository,
            new FakeTenantContext(),
            fixture.Templates);

        var result = await handler.Handle(
            new PreIssueContractCommand(fixture.Brand.Id, 1500m, null, "39053344705", null, null),
            CancellationToken.None);

        Assert.Equal(ContractStatus.PreIssued.ToString(), result.Status);
        Assert.Equal("Acme Ltd", result.CompanyName);
        Assert.Equal("Av Paulista", result.Street);
        Assert.Equal("100", result.Number);
        Assert.Equal("São Paulo", result.City);
        Assert.Equal("SP", result.State);
        Assert.Equal("01310100", result.ZipCode);
        Assert.Equal(ContractTemplateKeys.DefaultMinimumTerm, result.MinimumTerm);
        Assert.Equal(ContractTemplateKeys.PrestacaoServicos, result.TemplateKey);
        Assert.Equal(1500m, result.Amount);
        Assert.Contains("Acme Ltd", result.BodyMarkdown);
        Assert.Contains("390.533.447-05", result.BodyMarkdown);
        Assert.Contains("Av Paulista", result.BodyMarkdown);
        Assert.Contains("Widgets", result.BodyMarkdown);
        Assert.Contains("1.500,00", result.BodyMarkdown);
        Assert.Contains("12 (doze) meses", result.BodyMarkdown);
    }

    [Fact]
    public async Task Update_Persists_Body_Period_And_Address()
    {
        var fixture = Seed(withPreIssued: true);
        var handler = new UpdateContractCommandHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            fixture.Repository,
            new FakeTenantContext());

        var result = await handler.Handle(
            new UpdateContractCommand(
                fixture.Contract!.Id,
                2000m,
                fixture.Tenant.Document!,
                "39053344705",
                "Widgets",
                "9",
                "# Corpo editado",
                "24 (vinte e quatro) meses",
                "Nova Empresa",
                "Rua B",
                "50",
                "Canoas",
                "RS",
                "92010-000"),
            CancellationToken.None);

        Assert.Equal("# Corpo editado", result.BodyMarkdown);
        Assert.Equal("24 (vinte e quatro) meses", result.MinimumTerm);
        Assert.Equal("Nova Empresa", result.CompanyName);
        Assert.Equal("Rua B", result.Street);
        Assert.Equal("50", result.Number);
        Assert.Equal("Canoas", result.City);
        Assert.Equal("RS", result.State);
        Assert.Equal("92010000", result.ZipCode);
        Assert.Equal(2000m, result.Amount);
    }

    [Fact]
    public async Task Issue_Blocked_When_Address_Incomplete()
    {
        var fixture = Seed(withPreIssued: true, includeAddress: false);
        var documents = new FakeContractDocumentGenerator();
        var handler = new IssueContractCommandHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            fixture.Repository,
            new FakeTenantContext(),
            documents);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new IssueContractCommand(fixture.Contract!.Id), CancellationToken.None));

        Assert.Contains("cannot be issued", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(documents.PdfModels);
        Assert.Empty(fixture.Repository.Query<ContractFile>());
    }

    [Fact]
    public async Task Issue_Generates_And_Persists_Pdf()
    {
        var fixture = Seed(withPreIssued: true);
        var documents = new FakeContractDocumentGenerator();
        var handler = new IssueContractCommandHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            fixture.Repository,
            new FakeTenantContext(),
            documents);

        var result = await handler.Handle(new IssueContractCommand(fixture.Contract!.Id), CancellationToken.None);

        Assert.Equal(ContractStatus.Issued.ToString(), result.Status);
        Assert.Single(documents.PdfModels);
        Assert.True(documents.PdfModels[0].StampFormarcaSignature);
        var file = Assert.Single(fixture.Repository.Query<ContractFile>());
        Assert.Equal(ContractFileKind.Issued, file.Kind);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(file.Content));
    }

    [Fact]
    public async Task Sign_Stamps_Client_Png_Persists_Signed_Pdf_And_Creates_Invoice()
    {
        var fixture = Seed(withPreIssued: true);
        var documents = new FakeContractDocumentGenerator();
        var issuer = new IssueContractCommandHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            fixture.Repository,
            new FakeTenantContext(),
            documents);
        await issuer.Handle(new IssueContractCommand(fixture.Contract!.Id), CancellationToken.None);

        var handler = new SignContractCommandHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            fixture.Repository,
            new FakeTenantContext(),
            documents);

        var result = await handler.Handle(
            new SignContractCommand(fixture.Contract.Id, TinyPngBase64),
            CancellationToken.None);

        Assert.Equal(ContractStatus.Signed.ToString(), result.Status);
        Assert.NotNull(result.ClientSignedAt);
        Assert.NotNull(result.ClientSignaturePng);
        Assert.Equal(2, documents.PdfModels.Count);
        Assert.NotNull(documents.PdfModels[1].ClientSignaturePng);
        Assert.Contains(
            fixture.Repository.Query<ContractFile>(),
            f => f.Kind == ContractFileKind.Signed && f.Content.Length > 0);
        Assert.Contains(
            fixture.Repository.Query<Invoice>(),
            i => i.ContractId == fixture.Contract.Id && i.Status == InvoiceStatus.PendingPayment);
    }

    [Fact]
    public async Task Sign_Rejects_Non_Png()
    {
        var fixture = Seed(withPreIssued: true);
        var documents = new FakeContractDocumentGenerator();
        await new IssueContractCommandHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            fixture.Repository,
            new FakeTenantContext(),
            documents).Handle(new IssueContractCommand(fixture.Contract!.Id), CancellationToken.None);

        var handler = new SignContractCommandHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            fixture.Repository,
            new FakeTenantContext(),
            documents);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new SignContractCommand(fixture.Contract.Id, Convert.ToBase64String("not-a-png"u8.ToArray())), CancellationToken.None));
    }

    [Fact]
    public void Placeholders_Merge_Formats_Document_Currency_And_Class_Count()
    {
        var merged = ContractPlaceholders.Merge(
            "X {{contratanteNome}} {{cnpj}} {{quantidadeClasses}} {{valor}} {{cep}} {{periodo}}",
            ContractPlaceholders.From(
                "Acme",
                "62500545000114",
                "Rua",
                "1",
                "Cidade",
                "RS",
                "94931130",
                "Marca",
                "1, 9, 35",
                1500m,
                ContractTemplateKeys.DefaultMinimumTerm));

        Assert.Contains("Acme", merged);
        Assert.Contains("62.500.545/0001-14", merged);
        Assert.Contains("3", merged);
        Assert.Contains("1.500,00", merged);
        Assert.Contains("94931-130", merged);
        Assert.Contains("12 (doze) meses", merged);
    }

    private static Fixture Seed(bool withPreIssued = false, bool includeAddress = true)
    {
        var repository = new InMemoryRepository();
        var address = Address.Create("01310100", "Av Paulista", "100", null, "Bela Vista", "São Paulo", "SP");
        var tenant = Tenant.Register(
            "Acme Ltd",
            "acme-ltd",
            "maria@acme.com",
            "11987654321",
            "39053344705",
            address);
        tenant.Approve();
        repository.Add(tenant);

        var brand = Brand.Create(tenant.Id, "Widgets", "Widget services", [9]);
        repository.Add(brand);

        Contract? contract = null;
        if (withPreIssued)
        {
            contract = Contract.PreIssue(
                tenant.Id,
                brand.Id,
                "FM-2026-000001",
                tenant.Document!,
                "39053344705",
                brand.Name,
                brand.ClassesSnapshot(),
                1000m,
                ContractTemplateKeys.PrestacaoServicos,
                includeAddress ? "Nome Acme Ltd CNPJ 390.533.447-05" : "# Rascunho",
                includeAddress ? tenant.Name : "",
                includeAddress ? address.Street : "",
                includeAddress ? address.Number : "",
                includeAddress ? address.City : "",
                includeAddress ? address.State : "",
                includeAddress ? address.ZipCode : "",
                ContractTemplateKeys.DefaultMinimumTerm);
            repository.Add(contract);
        }

        return new Fixture(repository, tenant, brand, contract, new FakeContractTemplateProvider());
    }

    private sealed record Fixture(
        InMemoryRepository Repository,
        Tenant Tenant,
        Brand Brand,
        Contract? Contract,
        FakeContractTemplateProvider Templates);
}
