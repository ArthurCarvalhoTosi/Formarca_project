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
