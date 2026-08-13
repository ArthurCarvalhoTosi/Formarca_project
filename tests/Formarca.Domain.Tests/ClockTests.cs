using Formarca.Domain.Common;
using Formarca.Domain.Tenancy;

namespace Formarca.Domain.Tests;

public sealed class FakeClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; set; } = utcNow;
}

public class ClockTests
{
    [Fact]
    public void SoftDelete_Uses_Ambient_Clock()
    {
        var frozen = new DateTime(2026, 8, 12, 15, 0, 0, DateTimeKind.Utc);
        using (Clock.Use(new FakeClock(frozen)))
        {
            var tenant = Tenant.Create("Acme", "acme");
            tenant.SoftDelete();

            Assert.Equal(frozen, tenant.CreatedAtUtc);
            Assert.Equal(frozen, tenant.DeletedAtUtc);
            Assert.Equal(frozen, tenant.UpdatedAtUtc);
            Assert.True(tenant.IsDeleted);
        }
    }
}
