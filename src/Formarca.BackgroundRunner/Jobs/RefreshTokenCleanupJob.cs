using Formarca.Application.Common.Interfaces;
using Formarca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Formarca.BackgroundRunner.Jobs;

/// <summary>
/// Example background job: purge expired/revoked refresh tokens.
/// Jobs that touch tenant data should restore TenantId via ITenantContext.
/// </summary>
public sealed class RefreshTokenCleanupJob(
    IServiceScopeFactory scopeFactory,
    ILogger<RefreshTokenCleanupJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                tenantContext.SetIgnoreTenantFilters(true);

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var cutoff = DateTime.UtcNow.AddDays(-7);

                var stale = await db.RefreshTokens
                    .Where(t =>
                        (t.RevokedAtUtc != null && t.RevokedAtUtc < cutoff)
                        || (t.ExpiresAtUtc < cutoff))
                    .ToListAsync(stoppingToken);

                if (stale.Count > 0)
                {
                    db.RefreshTokens.RemoveRange(stale);
                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Removed {Count} stale refresh tokens", stale.Count);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Refresh token cleanup failed");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
