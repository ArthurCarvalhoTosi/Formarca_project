using Formarca.Application.IdentityAccess.Auth;
using Formarca.Application.Invoices.Commands;
using Formarca.Domain.Brands;
using Formarca.Domain.Contracts;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Invoices;
using Formarca.Domain.Tenancy;
using ProcessEntity = Formarca.Domain.Processes.Process;

namespace Formarca.Application.Tests;

public class InvoiceHandlerTests
{
    [Fact]
    public async Task Approve_Activates_Onboarding_Tenant_Promotes_Group_And_Creates_Process()
    {
        var fixture = await SeedOnboardingInvoiceAsync();
        var handler = new ApproveInvoiceCommandHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            fixture.Repository,
            new FakeTenantContext());

        var result = await handler.Handle(
            new ApproveInvoiceCommand(fixture.Invoice.Id, 1000m),
            CancellationToken.None);

        Assert.Equal(InvoiceStatus.Paid.ToString(), result.Status);
        Assert.Equal(TenantStatus.Active, fixture.Tenant.Status);
        Assert.Single(fixture.Repository.Query<ProcessEntity>());
        Assert.Equal(fixture.Contract.Id, fixture.Repository.Query<ProcessEntity>().Single().ContractId);
        Assert.Contains(
            fixture.Repository.Query<UserGroupMembership>(),
            m => m.UserId == fixture.UserId && m.GroupId == fixture.ClientUsersGroup.Id && !m.IsDeleted);
        Assert.DoesNotContain(
            fixture.Repository.Query<UserGroupMembership>(),
            m => m.UserId == fixture.UserId && m.GroupId == fixture.OnboardingGroup.Id && !m.IsDeleted);
    }

    [Fact]
    public async Task Second_Paid_Invoice_Does_Not_Recreate_Process_Or_Reactivate()
    {
        var fixture = await SeedOnboardingInvoiceAsync();
        var handler = new ApproveInvoiceCommandHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            fixture.Repository,
            new FakeTenantContext());

        await handler.Handle(new ApproveInvoiceCommand(fixture.Invoice.Id, 1000m), CancellationToken.None);
        var processId = fixture.Repository.Query<ProcessEntity>().Single().Id;

        await FunnelActivation.OnInvoicePaidAsync(fixture.Repository, fixture.Invoice, CancellationToken.None);

        Assert.Equal(TenantStatus.Active, fixture.Tenant.Status);
        Assert.Single(fixture.Repository.Query<ProcessEntity>());
        Assert.Equal(processId, fixture.Repository.Query<ProcessEntity>().Single().Id);
    }

    [Fact]
    public async Task Approve_Without_Received_Amount_Uses_Expected_Amount()
    {
        var fixture = await SeedOnboardingInvoiceAsync();
        var handler = new ApproveInvoiceCommandHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            fixture.Repository,
            new FakeTenantContext());

        var result = await handler.Handle(
            new ApproveInvoiceCommand(fixture.Invoice.Id, null),
            CancellationToken.None);

        Assert.Equal(1000m, result.ReceivedAmount);
        Assert.Equal(InvoiceStatus.Paid.ToString(), result.Status);
    }

    [Fact]
    public async Task Update_Settings_Accepts_Interest_Rates_Object_And_Json()
    {
        var repository = new InMemoryRepository();
        var rates = new Dictionary<string, decimal>
        {
            ["1"] = 1, ["2"] = 2, ["3"] = 3, ["4"] = 4, ["5"] = 5, ["6"] = 6,
            ["7"] = 7, ["8"] = 8, ["9"] = 9, ["10"] = 10, ["11"] = 11, ["12"] = 12,
            ["cardCash"] = 1.5m
        };
        var handler = new UpdateInvoiceSettingsCommandHandler(repository, new FakeTenantContext());

        var result = await handler.Handle(
            new UpdateInvoiceSettingsCommand(rates, null, 5, 2),
            CancellationToken.None);

        Assert.Equal(5, result.PixDiscount);
        Assert.Equal(2, result.GeneralDiscount);
        Assert.Equal(1.5m, result.InterestRates["cardCash"]);
        Assert.Equal(12, result.InterestRates["12"]);
        Assert.Contains("cardCash", result.InterestRatesJson);

        var json = InterestRates.Create(
            Enumerable.Range(1, 12).ToDictionary(i => i, i => (decimal)i),
            3m).ToJson();
        var fromJson = await handler.Handle(
            new UpdateInvoiceSettingsCommand(null, json, 1, 0),
            CancellationToken.None);
        Assert.Equal(3m, fromJson.InterestRates["cardCash"]);
        Assert.Equal(1, fromJson.PixDiscount);
    }

    private static async Task<Fixture> SeedOnboardingInvoiceAsync()
    {
        var repository = new InMemoryRepository();
        foreach (var (code, name) in PermissionCodes.Catalog)
        {
            repository.Add(Permission.Create(code, name));
        }

        var tenant = Tenant.Register(
            "Acme Ltd",
            "acme-ltd",
            "maria@acme.com",
            "11987654321",
            "39053344705",
            Address.Create("01310100", "Av Paulista", "100", null, "Bela Vista", "São Paulo", "SP"));
        tenant.Approve();
        repository.Add(tenant);

        await AuthTokenIssuer.EnsureDefaultTenantGroupsAsync(repository, tenant.Id, CancellationToken.None);
        var onboarding = repository.Query<Group>().Single(g => g.Name == DefaultGroupNames.ClientOnboarding);
        var clientUsers = repository.Query<Group>().Single(g => g.Name == DefaultGroupNames.ClientUsers);

        var userId = Guid.NewGuid();
        repository.Add(UserTenantMembership.Create(userId, tenant.Id, isDefault: true));
        repository.Add(UserGroupMembership.Create(userId, tenant.Id, onboarding.Id));

        var brand = Brand.Create(tenant.Id, "Acme", "Widgets", [9]);
        repository.Add(brand);

        var contract = Contract.PreIssue(
            tenant.Id,
            brand.Id,
            "FM-2026-000001",
            tenant.Document!,
            "39053344705",
            brand.Name,
            brand.ClassesSnapshot(),
            1000m);
        contract.Issue();
        contract.SignByClient();
        repository.Add(contract);

        var invoice = Invoice.CreatePending(tenant.Id, contract.Id, contract.Amount);
        invoice.SubmitPayment(PaymentMethod.Pix, 1, 1000m);
        repository.Add(invoice);

        return new Fixture(repository, tenant, contract, invoice, userId, onboarding, clientUsers);
    }

    private sealed record Fixture(
        InMemoryRepository Repository,
        Tenant Tenant,
        Contract Contract,
        Invoice Invoice,
        Guid UserId,
        Group OnboardingGroup,
        Group ClientUsersGroup);
}
