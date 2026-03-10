using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RestSharp; // RestSharp v112.1.0
using RestSharp.Authenticators;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Infrastructure.Configuration;

namespace MyHomeSolution.Infrastructure.Services;

public sealed class MailgunEmailService(
    HttpClient httpClient,
    IOptions<MailgunOptions> options,
    ILogger<MailgunEmailService> logger) : IEmailService
{
    private readonly MailgunOptions _options = options.Value;

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
        var options = new RestClientOptions("https://api.mailgun.net")
        {
            Authenticator = new HttpBasicAuthenticator("api", _options.ApiKey ?? "API_KEY")
        };

        var url = $"{_options.BaseUrl.TrimEnd('/')}/{_options.Domain}/messages";

        var recipientAddress = string.IsNullOrWhiteSpace(toName)
            ? toEmail
            : $"{toName} <{toEmail}>";

        var fromAddress = string.IsNullOrWhiteSpace(_options.FromName)
            ? _options.FromEmail
            : $"{_options.FromName} <{_options.FromEmail}>";

        var client = new RestClient(options);
        var request = new RestRequest($"/v3/{_options.Domain}/messages", Method.Post);
        request.AlwaysMultipartFormData = true;
        request.AddParameter("from", fromAddress);
        request.AddParameter("to", recipientAddress);
        request.AddParameter("subject", subject);
        request.AddParameter("html", htmlBody);
        RestResponse response = await client.ExecuteAsync(request);


        if (!response.IsSuccessStatusCode)
        {
            var body = response.Content?.ToString();
            logger.LogError(
                "Mailgun API returned {StatusCode}: {Body}",
                (int)response.StatusCode, body);
        }
        else
        {
            logger.LogInformation(
                "Email sent successfully to {ToEmail} with subject '{Subject}'",
                toEmail, subject);
        }
    }
}
