using Formarca.Application.Tenancy.Queries;
using Formarca.Domain.Common;
using Formarca.Domain.Tenancy;

namespace Formarca.Application.Tests;

public class TenantListWindowTests
{
    [Fact]
    public async Task Active_Tenants_Are_Limited_To_The_Creation_Window()
    {
        var now = new DateTime(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);
        Tenant recent;
        Tenant stale;
        Tenant pending;
        using (Clock.Use(new FakeClock(now.AddDays(-2))))
        {
            recent = Tenant.Create("Recente Ltda", "recente", "00.000.000/0001-91");
        }

        using (Clock.Use(new FakeClock(now.AddDays(-30))))
        {
            stale = Tenant.Create("Antigo Ltda", "antigo", "00.000.000/0001-92");
            pending = Tenant.Register(
                "Pendente Ltda",
                "pendente",
                "pendente@example.com",
                "11999999999",
                "00.000.000/0001-93",
                Address.Create("01001000", "Rua A", "1", null, "Centro", "São Paulo", "SP"));
        }

        var repository = new InMemoryRepository();
        repository.Add(recent);
        repository.Add(stale);
        repository.Add(pending);

        var result = await new ListTenantsQueryHandler(repository, new FakeTenantContext())
            .Handle(
                new ListTenantsQuery(
                    Statuses: [TenantStatus.Pending, TenantStatus.Onboarding, TenantStatus.Suspended, TenantStatus.Active],
                    ActiveSince: now.AddDays(-7)),
                CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, item => item.Id == recent.Id && item.Status == nameof(TenantStatus.Active));
        Assert.Contains(result.Items, item => item.Id == pending.Id && item.Status == nameof(TenantStatus.Pending));
    }
}
