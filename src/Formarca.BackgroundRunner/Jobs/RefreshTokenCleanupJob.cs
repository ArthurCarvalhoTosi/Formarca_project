using Cronos;
using Formarca.Application.Common.Interfaces;
using Formarca.Domain.Common;
using Formarca.Domain.IdentityAccess;
using Microsoft.EntityFrameworkCore;

namespace Formarca.BackgroundRunner.Jobs;

/// <summary>
/// Purges expired/revoked refresh tokens on a cron schedule (default: every 6 hours).
/// </summary>
public sealed class RefreshTokenCleanupJob(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<RefreshTokenCleanupJob> logger) : BackgroundService
{
    public const string CronConfigKey = "BackgroundJobs:RefreshTokenCleanupCron";
    public const string DefaultCron = "0 0 */6 * * *";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cron = configuration[CronConfigKey];
        if (string.IsNullOrWhiteSpace(cron))
        {
            cron = DefaultCron;
        }

        var expression = CronExpression.Parse(cron, CronFormat.IncludeSeconds);

        await RunCleanupAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var next = expression.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
            if (next is null)
            {
                break;
            }

            var delay = next.Value - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            await RunCleanupAsync(stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantContext.SetIgnoreTenantFilters(true);

            var clock = scope.ServiceProvider.GetRequiredService<IClock>();
            var repository = scope.ServiceProvider.GetRequiredService<IGenericRepository>();
            var cutoff = clock.UtcNow.AddDays(-7);

            var stale = await repository.Query<RefreshToken>()
                .Where(t =>
                    (t.RevokedAtUtc != null && t.RevokedAtUtc < cutoff)
                    || t.ExpiresAtUtc < cutoff)
                .ToListAsync(stoppingToken);

            if (stale.Count > 0)
            {
                await repository.BulkDeleteAsync(stale, stoppingToken);
                logger.LogInformation("Removed {Count} stale refresh tokens", stale.Count);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Refresh token cleanup failed");
        }
    }
}
