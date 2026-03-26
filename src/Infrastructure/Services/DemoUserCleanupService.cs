using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Infrastructure.Persistence;

namespace MyHomeSolution.Infrastructure.Services;

/// <summary>
/// Background service that runs every 5 minutes to check for expired demo users,
/// purge their data, send a thank-you email, and mark the DemoUser record as inactive.
/// </summary>
public sealed class DemoUserCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<DemoUserCleanupService> logger)
    : MonitoredBackgroundService<DemoUserCleanupService>(scopeFactory, logger),
      IMonitoredBackgroundService
{
    public static Guid ServiceId { get; } =
        Guid.Parse("a1b2c3d4-0005-0005-0005-000000000005");

    public static string ServiceName => "Demo User Cleanup";

    public static string ServiceDescription =>
        "Periodically checks for expired demo user sessions and purges all their data after 24 hours.";

    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit at startup so the rest of the app can finish initialising
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunMonitoredCycleAsync(ProcessExpiredDemoUsersAsync, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during demo user cleanup cycle");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task<string?> ProcessExpiredDemoUsersAsync(CancellationToken cancellationToken)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var emailQueue = scope.ServiceProvider.GetRequiredService<IEmailBackgroundQueue>();

        var now = dateTimeProvider.UtcNow;

        var expiredDemoUsers = await db.DemoUsers
            .Where(d => d.IsActive && d.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (expiredDemoUsers.Count == 0)
            return "No expired demo users found.";

        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeederService>();
        var purgedCount = 0;

        foreach (var demoUser in expiredDemoUsers)
        {
            try
            {
                logger.LogInformation(
                    "Cleaning up expired demo user: {Email} (ID: {UserId})",
                    demoUser.Email, demoUser.UserId);

                // Purge all data
                await seeder.PurgeUserDataAsync(demoUser.UserId, cancellationToken);

                // Mark as inactive in the DemoUsers tracking table
                demoUser.IsActive = false;
                await db.SaveChangesAsync(cancellationToken);

                // Send thank-you email
                var html = EmailTemplates.DemoExpired(demoUser.FullName);
                await emailQueue.EnqueueAsync(
                    new EmailMessage(
                        demoUser.Email,
                        demoUser.FullName,
                        "Your Demo Session Has Ended — MyHome",
                        html),
                    cancellationToken);

                purgedCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to clean up demo user {Email} ({UserId})",
                    demoUser.Email, demoUser.UserId);
            }
        }

        return $"Purged {purgedCount}/{expiredDemoUsers.Count} expired demo users.";
    }
}
