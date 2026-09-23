using Formarca.Application.Common.Exceptions;
using Formarca.Application.Processes.Commands;
using Formarca.Application.Processes.Queries;
using Formarca.Domain.Brands;
using Formarca.Domain.Processes;
using Formarca.Domain.Tenancy;

namespace Formarca.Application.Tests;

public class TrademarkDashboardTests
{
    [Fact]
    public async Task Changing_Status_Shows_On_The_Dashboard_And_In_History()
    {
        var tenant = Tenant.Create("Acme Ltd", "acme", "00.000.000/0001-91");
        var brand = Brand.Create(tenant.Id, "Delta", "Serviços de marca.");
        var process = Process.CreateGenerated(tenant.Id, brand.Id, Guid.NewGuid());
        var filed = TrademarkStatus.Create(
            "deposito-efetuado",
            "Depósito efetuado / Pedido protocolado",
            "O pedido foi recebido e aguarda a triagem inicial.",
            10,
            TrademarkStatusPhase.Filing,
            TrademarkStatusTone.Progress);
        var requirement = TrademarkStatus.Create(
            "exigencia-formal",
            "Exigência formal",
            "Há um prazo de 5 dias para cumprimento.",
            30,
            TrademarkStatusPhase.Filing,
            TrademarkStatusTone.Attention);

        var repository = new InMemoryRepository();
        repository.Add(tenant);
        repository.Add(brand);
        repository.Add(process);
        repository.Add(filed);
        repository.Add(requirement);

        var user = new FakeCurrentUser { IsPlatformAdmin = true };
        var tenantContext = new FakeTenantContext();
        var handler = new ChangeProcessTrademarkStatusCommandHandler(user, repository, tenantContext);

        var detail = await handler.Handle(
            new ChangeProcessTrademarkStatusCommand(process.Id, requirement.Id, "Falta a GRU.", "924567890"),
            CancellationToken.None);

        Assert.Equal(requirement.Id, detail.Status!.Id);
        Assert.Equal("924567890", detail.ProcessNumber);
        Assert.Equal("Delta", detail.BrandName);
        var entry = Assert.Single(detail.History);
        Assert.Equal("Falta a GRU.", entry.Note);
        Assert.Null(entry.FromStatusId);

        var dashboard = await new ListTrademarkDashboardQueryHandler(user, repository, tenantContext)
            .Handle(new ListTrademarkDashboardQuery(null, null, null, null), CancellationToken.None);

        var item = Assert.Single(dashboard.Items);
        Assert.Equal("Exigência formal", item.Status!.Title);
        Assert.Equal(1, dashboard.Kpis.Total);
        Assert.Equal(1, dashboard.Kpis.ActionRequired);
        Assert.Equal(0, dashboard.Kpis.WithoutStatus);
    }

    [Fact]
    public async Task Delete_Is_Blocked_When_A_Process_Uses_The_Status()
    {
        var status = TrademarkStatus.Create(
            "deferimento",
            "Deferimento do pedido de registro",
            "O pedido foi aprovado.",
            120,
            TrademarkStatusPhase.Decision,
            TrademarkStatusTone.Success);
        var process = Process.CreateGenerated(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        process.AssignTrademarkStatus(status.Id);

        var repository = new InMemoryRepository();
        repository.Add(status);
        repository.Add(process);

        var handler = new DeleteTrademarkStatusCommandHandler(
            new FakeCurrentUser { IsPlatformAdmin = true },
            repository);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new DeleteTrademarkStatusCommand(status.Id), CancellationToken.None));

        Assert.Contains("Deactivate", error.Message);
        Assert.False(status.IsDeleted);
    }

    [Fact]
    public async Task Open_Filter_Keeps_Pending_And_Important_Statuses()
    {
        var tenant = Tenant.Create("Acme Ltd", "acme", "00.000.000/0001-91");
        var brand = Brand.Create(tenant.Id, "Delta", "Serviços de marca.");
        var pending = Process.CreateGenerated(tenant.Id, brand.Id, Guid.NewGuid());
        var attention = Process.CreateGenerated(tenant.Id, brand.Id, Guid.NewGuid());
        var progress = Process.CreateGenerated(tenant.Id, brand.Id, Guid.NewGuid());
        var requirement = TrademarkStatus.Create(
            "exigencia-formal",
            "Exigência formal",
            "Há um prazo de 5 dias para cumprimento.",
            30,
            TrademarkStatusPhase.Filing,
            TrademarkStatusTone.Attention);
        var filed = TrademarkStatus.Create(
            "deposito-efetuado",
            "Depósito efetuado",
            "O pedido foi recebido.",
            10,
            TrademarkStatusPhase.Filing,
            TrademarkStatusTone.Progress);
        attention.AssignTrademarkStatus(requirement.Id);
        progress.AssignTrademarkStatus(filed.Id);

        var repository = new InMemoryRepository();
        repository.Add(tenant);
        repository.Add(brand);
        repository.Add(pending);
        repository.Add(attention);
        repository.Add(progress);
        repository.Add(requirement);
        repository.Add(filed);

        var dashboard = await new ListTrademarkDashboardQueryHandler(
                new FakeCurrentUser { IsPlatformAdmin = true },
                repository,
                new FakeTenantContext())
            .Handle(
                new ListTrademarkDashboardQuery(
                    null,
                    null,
                    null,
                    null,
                    Tones: [TrademarkStatusTone.Attention, TrademarkStatusTone.Danger, TrademarkStatusTone.Paused],
                    IncludeWithoutStatus: true),
                CancellationToken.None);

        Assert.Equal(2, dashboard.TotalCount);
        Assert.Contains(dashboard.Items, item => item.ProcessId == pending.Id && item.Status is null);
        Assert.Contains(dashboard.Items, item => item.Status?.Id == requirement.Id);
        Assert.Equal(3, dashboard.Kpis.Total);
    }
}
