using System.ComponentModel.DataAnnotations;

namespace MyHomeSolution.Infrastructure.Configuration;

public sealed class MailgunOptions
{
    public const string SectionName = "Mailgun";

    [Required]
    public string ApiKey { get; set; } = default!;

    [Required]
    public string Domain { get; set; } = default!;

    [Required, EmailAddress]
    public string FromEmail { get; set; } = default!;

    [Required]
    public string FromName { get; set; } = "MyHome";

    public string BaseUrl { get; set; } = "https://api.mailgun.net/v3";
}
