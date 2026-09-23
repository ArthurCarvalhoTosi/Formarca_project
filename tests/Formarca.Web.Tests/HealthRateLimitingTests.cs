using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Formarca.Web.Tests;

public sealed class FormarcaWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(HealthRateLimitOverrides());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(HealthRateLimitOverrides());
        });

        return base.CreateHost(builder);
    }

    private static Dictionary<string, string?> HealthRateLimitOverrides() =>
        new()
        {
            ["RateLimiting:Health:PermitLimit"] = "2",
            ["RateLimiting:Health:WindowSeconds"] = "60"
        };
}

public class HealthRateLimitingTests : IClassFixture<FormarcaWebApplicationFactory>
{
    private readonly FormarcaWebApplicationFactory _factory;

    public HealthRateLimitingTests(FormarcaWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_Returns_429_With_Retry_After_After_Permit_Limit()
    {
        using var client = _factory.CreateClient();

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 3; i++)
        {
            lastResponse?.Dispose();
            lastResponse = await client.GetAsync("/api/health");
        }

        Assert.NotNull(lastResponse);
        using (lastResponse)
        {
            Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
            Assert.True(
                lastResponse.Headers.TryGetValues("Retry-After", out var retryAfterValues)
                && retryAfterValues.Any(v => !string.IsNullOrWhiteSpace(v)));
        }
    }
}
