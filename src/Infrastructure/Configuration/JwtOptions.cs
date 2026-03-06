using System.ComponentModel.DataAnnotations;

namespace MyHomeSolution.Infrastructure.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32)]
    public string Key { get; set; } = default!;

    [Required]
    public string Issuer { get; set; } = default!;

    [Required]
    public string Audience { get; set; } = default!;

    [Range(1, 1440)]
    public int AccessTokenExpirationMinutes { get; set; } = 60;

    [Range(1, 365)]
    public int RefreshTokenExpirationDays { get; set; } = 30;
}
