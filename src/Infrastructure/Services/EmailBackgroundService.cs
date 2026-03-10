using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyHomeSolution.Application.Common.Interfaces;

namespace MyHomeSolution.Infrastructure.Services;

public sealed class EmailBackgroundService(
    IEmailBackgroundQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<EmailBackgroundService> logger)
    : MonitoredBackgroundService<EmailBackgroundService>(scopeFactory, logger),
      IMonitoredBackgroundService
{
    public static Guid ServiceId => BackgroundServiceSeeder.ServiceIds.EmailBackground;
    public static string ServiceName => "Email Sender";
    public static string ServiceDescription =>
        "Processes the background email queue and sends emails via the configured email provider.";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email background service started.");

        await foreach (var message in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await RunMonitoredCycleAsync(async ct =>
                {
                    using var scope = ScopeFactory.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    await emailService.SendEmailAsync(
                        message.ToEmail,
                        message.ToName,
                        message.Subject,
                        message.HtmlBody,
                        ct);

                    return $"Sent email to {message.ToEmail}: {message.Subject}";
                }, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex,
                    "Failed to send email to {ToEmail} with subject '{Subject}'",
                    message.ToEmail, message.Subject);
            }
        }

        logger.LogInformation("Email background service stopped.");
    }
}
