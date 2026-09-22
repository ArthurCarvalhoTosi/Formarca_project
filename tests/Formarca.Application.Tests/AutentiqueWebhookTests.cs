using Formarca.Application.Autentique;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Contracts.Commands;
using Formarca.Domain.Brands;
using Formarca.Domain.Contracts;
using Formarca.Domain.Invoices;
using Formarca.Domain.Tenancy;

namespace Formarca.Application.Tests;

public class AutentiqueWebhookTests
{
    [Fact]
    public void Hmac_Accepts_Matching_Hex_And_Rejects_Mismatch()
    {
        const string secret = "webhook-secret";
        const string body = """{"event":{"type":"document.finished"}}""";
        var hex = AutentiqueWebhookHmac.ComputeHex(body, secret);

        Assert.True(AutentiqueWebhookHmac.Verify(body, hex, secret));
        Assert.True(AutentiqueWebhookHmac.Verify(body, hex.ToUpperInvariant(), secret));
        Assert.False(AutentiqueWebhookHmac.Verify(body, "00" + hex[2..], secret));
        Assert.False(AutentiqueWebhookHmac.Verify(body, hex, "other-secret"));
        Assert.False(AutentiqueWebhookHmac.Verify(body, null, secret));
        Assert.False(AutentiqueWebhookHmac.Verify(body, hex, ""));
    }

    [Fact]
    public async Task Finished_Downloads_Signed_Pdf_Signs_Contract_And_Creates_Invoice()
    {
        var fixture = await IssuedAsync();
        var body = FinishedBody(fixture.Autentique.DocumentId);
        var signature = AutentiqueWebhookHmac.ComputeHex(body, fixture.Autentique.WebhookSecret);

        await new HandleAutentiqueWebhookCommandHandler(
            fixture.Autentique,
            fixture.Repository,
            new FakeTenantContext()).Handle(
            new HandleAutentiqueWebhookCommand(body, signature),
            CancellationToken.None);

        var contract = fixture.Repository.Query<Contract>().Single(c => c.Id == fixture.Contract.Id);
        Assert.Equal(ContractStatus.Signed, contract.Status);
        Assert.Equal(AutentiqueDocumentStatus.Finished, contract.AutentiqueStatus);
        Assert.Null(contract.ClientSignaturePng);
        Assert.Equal(["autentique-doc-1"], fixture.Autentique.Downloads);
        Assert.Contains(
            fixture.Repository.Query<ContractFile>(),
            f => f.Kind == ContractFileKind.Signed && f.Content.SequenceEqual(fixture.Autentique.SignedPdf));
        Assert.Contains(
            fixture.Repository.Query<Invoice>(),
            i => i.ContractId == contract.Id && i.Status == InvoiceStatus.PendingPayment);
    }

    [Fact]
    public async Task Finished_Is_Idempotent_When_Already_Signed()
    {
        var fixture = await IssuedAsync();
        var body = FinishedBody(fixture.Autentique.DocumentId);
        var signature = AutentiqueWebhookHmac.ComputeHex(body, fixture.Autentique.WebhookSecret);
        var handler = new HandleAutentiqueWebhookCommandHandler(
            fixture.Autentique,
            fixture.Repository,
            new FakeTenantContext());

        await handler.Handle(new HandleAutentiqueWebhookCommand(body, signature), CancellationToken.None);
        await handler.Handle(new HandleAutentiqueWebhookCommand(body, signature), CancellationToken.None);

        Assert.Single(fixture.Repository.Query<Invoice>().Where(i => i.ContractId == fixture.Contract.Id));
        Assert.Single(fixture.Autentique.Downloads);
    }

    [Fact]
    public async Task Rejected_Does_Not_Sign_Or_Create_Invoice()
    {
        var fixture = await IssuedAsync();
        var body =
            "{\"event\":{\"type\":\"document.rejected\",\"data\":{\"object\":{\"id\":\""
            + fixture.Autentique.DocumentId
            + "\"}}}}";
        var signature = AutentiqueWebhookHmac.ComputeHex(body, fixture.Autentique.WebhookSecret);

        await new HandleAutentiqueWebhookCommandHandler(
            fixture.Autentique,
            fixture.Repository,
            new FakeTenantContext()).Handle(
            new HandleAutentiqueWebhookCommand(body, signature),
            CancellationToken.None);

        var contract = fixture.Repository.Query<Contract>().Single(c => c.Id == fixture.Contract.Id);
        Assert.Equal(ContractStatus.Issued, contract.Status);
        Assert.Equal(AutentiqueDocumentStatus.Rejected, contract.AutentiqueStatus);
        Assert.Empty(fixture.Autentique.Downloads);
        Assert.Empty(fixture.Repository.Query<Invoice>());
        Assert.DoesNotContain(fixture.Repository.Query<ContractFile>(), f => f.Kind == ContractFileKind.Signed);
    }

    [Fact]
    public async Task Invalid_Hmac_Is_Unauthorized()
    {
        var fixture = await IssuedAsync();
        var body = FinishedBody(fixture.Autentique.DocumentId);

        await Assert.ThrowsAsync<UnauthorizedAppException>(() =>
            new HandleAutentiqueWebhookCommandHandler(
                fixture.Autentique,
                fixture.Repository,
                new FakeTenantContext()).Handle(
                new HandleAutentiqueWebhookCommand(body, "deadbeef"),
                CancellationToken.None));

        Assert.Equal(ContractStatus.Issued, fixture.Contract.Status);
        Assert.Empty(fixture.Repository.Query<Invoice>());
    }

    [Fact]
    public async Task Missing_Hmac_Header_Is_Unauthorized()
    {
        var fixture = await IssuedAsync();
        await Assert.ThrowsAsync<UnauthorizedAppException>(() =>
            new HandleAutentiqueWebhookCommandHandler(
                fixture.Autentique,
                fixture.Repository,
                new FakeTenantContext()).Handle(
                new HandleAutentiqueWebhookCommand(FinishedBody(fixture.Autentique.DocumentId), null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Unknown_Document_Is_Ignored()
    {
        var fixture = await IssuedAsync();
        var body = FinishedBody("unknown-doc");
        var signature = AutentiqueWebhookHmac.ComputeHex(body, fixture.Autentique.WebhookSecret);

        await new HandleAutentiqueWebhookCommandHandler(
            fixture.Autentique,
            fixture.Repository,
            new FakeTenantContext()).Handle(
            new HandleAutentiqueWebhookCommand(body, signature),
            CancellationToken.None);

        Assert.Equal(ContractStatus.Issued, fixture.Contract.Status);
        Assert.Empty(fixture.Repository.Query<Invoice>());
    }

    private static string FinishedBody(string documentId) =>
        "{\"event\":{\"type\":\"document.finished\",\"data\":{\"object\":{\"id\":\""
        + documentId
        + "\",\"files\":{\"signed\":\"https://example.com/signed.pdf\"}}}}}";

    private static async Task<Fixture> IssuedAsync()
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

        var contract = Contract.PreIssue(
            tenant.Id,
            brand.Id,
            "FM-2026-000001",
            tenant.Document!,
            "39053344705",
            brand.Name,
            brand.ClassesSnapshot(),
            1000m,
            ContractTemplateKeys.PrestacaoServicos,
            "# Contrato",
            tenant.Name,
            address.Street,
            address.Number,
            address.City,
            address.State,
            address.ZipCode,
            ContractTemplateKeys.DefaultMinimumTerm);
        repository.Add(contract);

        var autentique = new FakeAutentiqueClient();
        await new IssueContractCommandHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            repository,
            new FakeTenantContext(),
            new FakeContractDocumentGenerator(),
            autentique).Handle(new IssueContractCommand(contract.Id), CancellationToken.None);

        autentique.Downloads.Clear();
        return new Fixture(repository, contract, autentique);
    }

    private sealed record Fixture(InMemoryRepository Repository, Contract Contract, FakeAutentiqueClient Autentique);
}
