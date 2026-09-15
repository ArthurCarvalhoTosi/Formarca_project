using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Models;
using Formarca.Application.Processes.Queries;
using Formarca.Domain.Brands;
using Formarca.Domain.Contracts;
using Formarca.Domain.Invoices;
using Formarca.Domain.Processes;
using Formarca.Domain.Tenancy;
using ProcessEntity = Formarca.Domain.Processes.Process;

namespace Formarca.Application.Tests;

public class ProcessDashboardQueryTests
{
    [Fact]
    public async Task Dashboard_Groups_By_Brand_And_Includes_Brands_Without_Process()
    {
        var seed = Seed();
        var handler = new ListProcessesDashboardQueryHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            seed.Repository,
            new FakeTenantContext());

        var result = await handler.Handle(
            new ListProcessesDashboardQuery([seed.Acme.Id], null, null, 1, 20),
            CancellationToken.None);

        Assert.Equal(4, result.TotalCount);
        Assert.Equal(4, result.Items.Count);
        Assert.Equal(4, result.Kpis.Brands);
        Assert.Equal(2, result.Kpis.ContractsInProgress);
        Assert.Equal(1, result.Kpis.InvoicesPending);
        Assert.Equal(1, result.Kpis.ProcessesGenerated);
        Assert.Equal("Acme Ltd", result.Items[0].TenantName);

        var byName = result.Items.ToDictionary(i => i.BrandName);
        Assert.Equal(ProcessFunnelStage.Brand, byName["Alpha"].FunnelStage);
        Assert.Null(byName["Alpha"].Contract);
        Assert.Equal(ProcessFunnelStage.Contract, byName["Bravo"].FunnelStage);
        Assert.NotNull(byName["Bravo"].Contract);
        Assert.Null(byName["Bravo"].Invoice);
        Assert.Equal(ProcessFunnelStage.Invoice, byName["Charlie"].FunnelStage);
        Assert.Equal(InvoiceStatus.AwaitingApproval.ToString(), byName["Charlie"].Invoice!.Status);
        Assert.Equal(1000m, byName["Charlie"].Invoice!.ExpectedAmount);
        Assert.Equal(ProcessFunnelStage.Process, byName["Delta"].FunnelStage);
        Assert.NotNull(byName["Delta"].Process);
        Assert.Equal(ProcessStatus.Generated.ToString(), byName["Delta"].Process!.Status);
    }

    [Fact]
    public async Task Dashboard_Filters_By_Funnel_Stage_Without_Changing_Kpis()
    {
        var seed = Seed();
        var handler = new ListProcessesDashboardQueryHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            seed.Repository,
            new FakeTenantContext());

        var result = await handler.Handle(
            new ListProcessesDashboardQuery([seed.Acme.Id], null, [ProcessFunnelStage.Brand], 1, 20),
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Alpha", result.Items[0].BrandName);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(4, result.Kpis.Brands);
        Assert.Equal(1, result.Kpis.ProcessesGenerated);
    }

    [Fact]
    public async Task Dashboard_Filters_By_Multiple_Funnel_Stages()
    {
        var seed = Seed();
        var handler = new ListProcessesDashboardQueryHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            seed.Repository,
            new FakeTenantContext());

        var result = await handler.Handle(
            new ListProcessesDashboardQuery(
                [seed.Acme.Id],
                null,
                [ProcessFunnelStage.Brand, ProcessFunnelStage.Process],
                1,
                20),
            CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(["Alpha", "Delta"], result.Items.Select(i => i.BrandName).ToArray());
        Assert.Equal(4, result.Kpis.Brands);
    }

    [Fact]
    public async Task Dashboard_Paginates_Brand_Cards()
    {
        var seed = Seed();
        var handler = new ListProcessesDashboardQueryHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            seed.Repository,
            new FakeTenantContext());

        var page1 = await handler.Handle(
            new ListProcessesDashboardQuery([seed.Acme.Id], null, null, 1, 2),
            CancellationToken.None);
        var page2 = await handler.Handle(
            new ListProcessesDashboardQuery([seed.Acme.Id], null, null, 2, 2),
            CancellationToken.None);

        Assert.Equal(4, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(["Alpha", "Bravo"], page1.Items.Select(i => i.BrandName).ToArray());
        Assert.Equal(["Charlie", "Delta"], page2.Items.Select(i => i.BrandName).ToArray());
        Assert.Equal(1, page1.Page);
        Assert.Equal(2, page1.PageSize);
    }

    [Fact]
    public async Task Dashboard_Filters_By_Brand_And_Admin_Sees_All_Tenants()
    {
        var seed = Seed();
        var handler = new ListProcessesDashboardQueryHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            seed.Repository,
            new FakeTenantContext());

        var byBrand = await handler.Handle(
            new ListProcessesDashboardQuery(null, [seed.Alpha.Id], null, 1, 20),
            CancellationToken.None);
        Assert.Single(byBrand.Items);
        Assert.Equal(seed.Alpha.Id, byBrand.Items[0].BrandId);

        var allTenants = await handler.Handle(
            new ListProcessesDashboardQuery(null, null, null, 1, 20),
            CancellationToken.None);
        Assert.Equal(5, allTenants.TotalCount);
        Assert.Contains(allTenants.Items, i => i.BrandName == "Other Brand");
    }

    [Fact]
    public async Task Dashboard_Non_Admin_Is_Scoped_To_Current_Tenant()
    {
        var seed = Seed();
        var handler = new ListProcessesDashboardQueryHandler(
            new FakeCurrentUser { IsPlatformAdmin = false, TenantId = seed.Acme.Id },
            seed.Repository,
            new FakeTenantContext());

        var result = await handler.Handle(
            new ListProcessesDashboardQuery(null, null, null, 1, 20),
            CancellationToken.None);

        Assert.Equal(4, result.TotalCount);
        Assert.DoesNotContain(result.Items, i => i.TenantId == seed.Other.Id);
    }

    [Fact]
    public async Task Dashboard_Non_Admin_Cannot_Query_Another_Tenant()
    {
        var seed = Seed();
        var handler = new ListProcessesDashboardQueryHandler(
            new FakeCurrentUser { IsPlatformAdmin = false, TenantId = seed.Acme.Id },
            seed.Repository,
            new FakeTenantContext());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new ListProcessesDashboardQuery([seed.Other.Id], null, null, 1, 20),
                CancellationToken.None));
    }

    private static SeedData Seed()
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

        var alpha = Brand.Create(acme.Id, "Alpha", "No contract yet", [9]);
        var bravo = Brand.Create(acme.Id, "Bravo", "Contract only", [9]);
        var charlie = Brand.Create(acme.Id, "Charlie", "Invoice pending", [9]);
        var delta = Brand.Create(acme.Id, "Delta", "Process generated", [9]);
        var otherBrand = Brand.Create(other.Id, "Other Brand", "Other tenant", [35]);
        repository.Add(alpha);
        repository.Add(bravo);
        repository.Add(charlie);
        repository.Add(delta);
        repository.Add(otherBrand);

        var bravoContract = PreIssue(acme, bravo, "FM-2026-000001");
        var charlieContract = PreIssue(acme, charlie, "FM-2026-000002");
        IssueAndSign(charlieContract);
        var deltaContract = PreIssue(acme, delta, "FM-2026-000003");
        IssueAndSign(deltaContract);
        repository.Add(bravoContract);
        repository.Add(charlieContract);
        repository.Add(deltaContract);

        var charlieInvoice = Invoice.CreatePending(acme.Id, charlieContract.Id, 1000m);
        charlieInvoice.SubmitPayment(PaymentMethod.Pix, 1, 1000m);
        var deltaInvoice = Invoice.CreatePending(acme.Id, deltaContract.Id, 1000m);
        deltaInvoice.SubmitPayment(PaymentMethod.Pix, 1, 1000m);
        deltaInvoice.Approve(1000m);
        repository.Add(charlieInvoice);
        repository.Add(deltaInvoice);

        repository.Add(ProcessEntity.CreateGenerated(acme.Id, delta.Id, deltaContract.Id));

        return new SeedData(repository, acme, other, alpha);
    }

    private static Contract PreIssue(Tenant tenant, Brand brand, string code) =>
        Contract.PreIssue(
            tenant.Id,
            brand.Id,
            code,
            tenant.Document!,
            "39053344705",
            brand.Name,
            brand.ClassesSnapshot(),
            1000m,
            bodyMarkdown: "# Contrato",
            companyName: tenant.Name,
            street: tenant.Address!.Street,
            number: tenant.Address.Number,
            city: tenant.Address.City,
            state: tenant.Address.State,
            zipCode: tenant.Address.ZipCode);

    private static void IssueAndSign(Contract contract)
    {
        contract.Issue();
        contract.SignByClient();
    }

    private sealed record SeedData(
        InMemoryRepository Repository,
        Tenant Acme,
        Tenant Other,
        Brand Alpha);
}
