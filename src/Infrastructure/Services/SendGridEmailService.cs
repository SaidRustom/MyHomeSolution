using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Infrastructure.Configuration;

namespace MyHomeSolution.Infrastructure.Services;

public sealed class SendGridEmailService(
    HttpClient httpClient,
    IOptions<SendGridOptions> options,
    ILogger<SendGridEmailService> logger) : IEmailService
{
    private const string SendGridApiUrl = "https://api.sendgrid.com/v3/mail/send";

    private readonly SendGridOptions _options = options.Value;

    public Task SendEmailAsync(
        string toEmail, string subject, string htmlBody,
        CancellationToken cancellationToken = default)
    {
        return SendEmailAsync(toEmail, null, subject, htmlBody, cancellationToken);
    }

    public async Task SendEmailAsync(
        string toEmail, string? toName, string subject, string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            personalizations = new[]
            {
                new
                {
                    to = new[]
                    {
                        new
                        {
                            email = toEmail,
                            name = toName ?? toEmail
                        }
                    }
                }
            },
            from = new
            {
                email = _options.FromEmail,
                name = _options.FromName
            },
            subject,
            content = new[]
            {
                new
                {
                    type = "text/html",
                    value = htmlBody
                }
            }
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, SendGridApiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "SendGrid API returned {StatusCode}: {Body}",
                (int)response.StatusCode, body);
        }
        else
        {
            logger.LogInformation(
                "Email sent successfully to {ToEmail} with subject '{Subject}'",
                toEmail, subject);
        }

        response.EnsureSuccessStatusCode();
    }
}
