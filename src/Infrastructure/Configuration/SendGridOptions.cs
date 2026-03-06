using System.ComponentModel.DataAnnotations;

namespace MyHomeSolution.Infrastructure.Configuration;

public sealed class SendGridOptions
{
    public const string SectionName = "SendGrid";

    [Required]
    public string ApiKey { get; set; } = default!;

    [Required, EmailAddress]
    public string FromEmail { get; set; } = default!;

    [Required]
    public string FromName { get; set; } = "MyHome";
}
