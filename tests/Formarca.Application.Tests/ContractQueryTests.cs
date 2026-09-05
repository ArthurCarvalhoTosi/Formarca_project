using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Contracts.Queries;
using Formarca.Domain.Brands;
using Formarca.Domain.Contracts;
using Formarca.Domain.Tenancy;

namespace Formarca.Application.Tests;

public class ContractQueryTests
{
    [Fact]
    public async Task List_Filters_By_Client_Name_Code_Brand_Document_Status_And_Dates()
    {
        var repository = Seed();
        var match = repository.Query<Contract>().Single(c => c.Code == "FM-2026-000001");
        var handler = new ListContractsQueryHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            repository,
            new FakeTenantContext());

        var byClient = await handler.Handle(
            new ListContractsQuery(null, null, null, null, null, "Acme", null, null, null, null),
            CancellationToken.None);
        Assert.Single(byClient.Items);
        Assert.Equal(match.Id, byClient.Items[0].Id);
        Assert.Equal("Acme Ltd", byClient.Items[0].TenantName);

        var byCode = await handler.Handle(
            new ListContractsQuery(null, null, null, "000001", null, null, null, null, null, null),
            CancellationToken.None);
        Assert.Single(byCode.Items);

        var byBrand = await handler.Handle(
            new ListContractsQuery(null, null, null, null, null, null, "Widgets", null, null, null),
            CancellationToken.None);
        Assert.Single(byBrand.Items);

        var byDocument = await handler.Handle(
            new ListContractsQuery(null, null, null, null, null, null, null, "390.533.447-05", null, null),
            CancellationToken.None);
        Assert.Single(byDocument.Items);

        var byUnknownDocument = await handler.Handle(
            new ListContractsQuery(null, null, null, null, null, null, null, "00000000000", null, null),
            CancellationToken.None);
        Assert.Empty(byUnknownDocument.Items);

        var byCompanyDocument = await handler.Handle(
            new ListContractsQuery(null, null, null, null, null, null, null, "39053344705", null, null),
            CancellationToken.None);
        Assert.Single(byCompanyDocument.Items);

        var byStatus = await handler.Handle(
            new ListContractsQuery(null, null, ContractStatus.PreIssued, null, null, null, null, null, null, null),
            CancellationToken.None);
        Assert.Equal(2, byStatus.Items.Count);

        var from = match.GeneratedAtUtc.AddMinutes(-1);
        var to = match.GeneratedAtUtc.AddMinutes(1);
        var byDates = await handler.Handle(
            new ListContractsQuery(null, null, null, null, null, null, null, null, from, to),
            CancellationToken.None);
        Assert.Equal(2, byDates.Items.Count);
    }

    [Fact]
    public async Task Preview_Returns_Pdf_For_PreIssued()
    {
        var repository = Seed();
        var contract = repository.Query<Contract>().Single(c => c.Code == "FM-2026-000001");
        var documents = new FakeContractDocumentGenerator();
        var handler = new PreviewContractQueryHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            repository,
            new FakeTenantContext(),
            documents,
            new FakeAutentiqueClient());

        var file = await handler.Handle(new PreviewContractQuery(contract.Id), CancellationToken.None);

        Assert.Equal("application/pdf", file.ContentType);
        Assert.Contains("preview.pdf", file.FileName);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(file.Content));
        Assert.False(documents.PdfModels[0].StampFormarcaSignature);
    }

    [Fact]
    public async Task Preview_Issued_Uses_Autentique_Pdf_When_Available()
    {
        var repository = Seed();
        var contract = repository.Query<Contract>().Single(c => c.Code == "FM-2026-000001");
        contract.UpdateSnapshot(
            contract.CompanyDocument,
            contract.RepresentativeDocument,
            contract.BrandName,
            contract.Classes,
            contract.Amount,
            "Acme Ltd",
            "Av Paulista",
            "100",
            "São Paulo",
            "SP",
            "01310100",
            ContractTemplateKeys.DefaultMinimumTerm,
            "# Contrato");
        contract.Issue();
        contract.AttachAutentiqueSigning("autentique-doc-1", "signer-1", "https://assina.ae/x");
        repository.Add(ContractFile.CreatePdf(
            contract.TenantId,
            contract.Id,
            ContractFileKind.Issued,
            $"{contract.Code}.pdf",
            "%PDF-issued-unsigned"u8.ToArray()));

        var documents = new FakeContractDocumentGenerator();
        var autentique = new FakeAutentiqueClient { CurrentPdfAvailable = true };
        var handler = new PreviewContractQueryHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            repository,
            new FakeTenantContext(),
            documents,
            autentique);

        var file = await handler.Handle(new PreviewContractQuery(contract.Id), CancellationToken.None);

        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("%PDF-autentique-signed", System.Text.Encoding.ASCII.GetString(file.Content));
        Assert.Empty(documents.PdfModels);
        Assert.Equal(["autentique-doc-1"], autentique.Downloads);
    }

    [Fact]
    public async Task Preview_Issued_Without_Autentique_Pdf_Keeps_Persisted_File()
    {
        var repository = Seed();
        var contract = repository.Query<Contract>().Single(c => c.Code == "FM-2026-000001");
        contract.UpdateSnapshot(
            contract.CompanyDocument,
            contract.RepresentativeDocument,
            contract.BrandName,
            contract.Classes,
            contract.Amount,
            "Acme Ltd",
            "Av Paulista",
            "100",
            "São Paulo",
            "SP",
            "01310100",
            ContractTemplateKeys.DefaultMinimumTerm,
            "# Contrato");
        contract.Issue();
        repository.Add(ContractFile.CreatePdf(
            contract.TenantId,
            contract.Id,
            ContractFileKind.Issued,
            $"{contract.Code}.pdf",
            "%PDF-issued-unsigned"u8.ToArray()));

        var documents = new FakeContractDocumentGenerator();
        var handler = new PreviewContractQueryHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            repository,
            new FakeTenantContext(),
            documents,
            new FakeAutentiqueClient());

        var file = await handler.Handle(new PreviewContractQuery(contract.Id), CancellationToken.None);

        Assert.Equal("%PDF-issued-unsigned", System.Text.Encoding.ASCII.GetString(file.Content));
        Assert.Empty(documents.PdfModels);
    }

    [Fact]
    public async Task Preview_Signed_Returns_Persisted_Signed_Pdf()
    {
        var repository = Seed();
        var contract = repository.Query<Contract>().Single(c => c.Code == "FM-2026-000001");
        contract.UpdateSnapshot(
            contract.CompanyDocument,
            contract.RepresentativeDocument,
            contract.BrandName,
            contract.Classes,
            contract.Amount,
            "Acme Ltd",
            "Av Paulista",
            "100",
            "São Paulo",
            "SP",
            "01310100",
            ContractTemplateKeys.DefaultMinimumTerm,
            "# Contrato");
        contract.Issue();
        contract.SignByClient();
        repository.Add(ContractFile.CreatePdf(
            contract.TenantId,
            contract.Id,
            ContractFileKind.Signed,
            $"{contract.Code}.pdf",
            "%PDF-autentique-signed"u8.ToArray()));

        var documents = new FakeContractDocumentGenerator();
        var handler = new PreviewContractQueryHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            repository,
            new FakeTenantContext(),
            documents,
            new FakeAutentiqueClient());

        var file = await handler.Handle(new PreviewContractQuery(contract.Id), CancellationToken.None);

        Assert.Equal("%PDF-autentique-signed", System.Text.Encoding.ASCII.GetString(file.Content));
        Assert.Empty(documents.PdfModels);
    }

    [Fact]
    public async Task Download_Returns_Persisted_Pdf_Only()
    {
        var repository = Seed();
        var contract = repository.Query<Contract>().Single(c => c.Code == "FM-2026-000001");
        contract.UpdateSnapshot(
            contract.CompanyDocument,
            contract.RepresentativeDocument,
            contract.BrandName,
            contract.Classes,
            contract.Amount,
            "Acme Ltd",
            "Av Paulista",
            "100",
            "São Paulo",
            "SP",
            "01310100",
            ContractTemplateKeys.DefaultMinimumTerm,
            "# Contrato");
        contract.Issue();
        repository.Add(ContractFile.CreatePdf(
            contract.TenantId,
            contract.Id,
            ContractFileKind.Issued,
            $"{contract.Code}.pdf",
            "%PDF-issued-persisted"u8.ToArray()));

        var documents = new FakeContractDocumentGenerator();
        var handler = new DownloadContractQueryHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            repository,
            new FakeTenantContext(),
            documents);

        var file = await handler.Handle(new DownloadContractQuery(contract.Id, "pdf"), CancellationToken.None);

        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("%PDF-issued-persisted", System.Text.Encoding.ASCII.GetString(file.Content));
        Assert.Empty(documents.PdfModels);

        var docx = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new DownloadContractQuery(contract.Id, "docx"), CancellationToken.None));
        Assert.Contains("PDF only", docx.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preview_Rejected_When_Cancelled()
    {
        var repository = Seed();
        var contract = repository.Query<Contract>().Single(c => c.Code == "FM-2026-000001");
        contract.Cancel();
        var handler = new PreviewContractQueryHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            repository,
            new FakeTenantContext(),
            new FakeContractDocumentGenerator(),
            new FakeAutentiqueClient());

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new PreviewContractQuery(contract.Id), CancellationToken.None));
    }

    private static IGenericRepository Seed()
    {
        var repository = new InMemoryRepository();
        var acme = Tenant.Register(
            "Acme Ltd",
            "acme-ltd",
            "maria@acme.com",
            "11987654321",
            "39053344705",
            Address.Create("01310100", "Av Paulista", "100", null, "Bela Vista", "São Paulo", "SP"));
        acme.Approve();
        var other = Tenant.Register(
            "Other Ltd",
            "other-ltd",
            "other@acme.com",
            "11987654321",
            "52998224725",
            Address.Create("01310100", "Av Paulista", "100", null, "Bela Vista", "São Paulo", "SP"));
        other.Approve();
        repository.Add(acme);
        repository.Add(other);

        var acmeBrand = Brand.Create(acme.Id, "Widgets", "Widget services", [9]);
        var otherBrand = Brand.Create(other.Id, "Gadgets", "Gadget services", [35]);
        repository.Add(acmeBrand);
        repository.Add(otherBrand);

        repository.Add(Contract.PreIssue(
            acme.Id, acmeBrand.Id, "FM-2026-000001", acme.Document!, "39053344705",
            acmeBrand.Name, acmeBrand.ClassesSnapshot(), 1000m));
        repository.Add(Contract.PreIssue(
            other.Id, otherBrand.Id, "FM-2026-000002", other.Document!, "52998224725",
            otherBrand.Name, otherBrand.ClassesSnapshot(), 2000m));
        return repository;
    }
}
