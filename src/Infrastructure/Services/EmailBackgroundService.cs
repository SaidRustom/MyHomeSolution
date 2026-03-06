using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyHomeSolution.Application.Common.Interfaces;

namespace MyHomeSolution.Infrastructure.Services;

public sealed class EmailBackgroundService(
    IEmailBackgroundQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<EmailBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email background service started.");

        await foreach (var message in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                await emailService.SendEmailAsync(
                    message.ToEmail,
                    message.ToName,
                    message.Subject,
                    message.HtmlBody,
                    stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to send email to {ToEmail} with subject '{Subject}'",
                    message.ToEmail, message.Subject);
            }
        }

        logger.LogInformation("Email background service stopped.");
    }
}
