using System.ComponentModel.DataAnnotations;

namespace MyHomeSolution.Infrastructure.Configuration;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    [Required]
    public string ApiKey { get; set; } = default!;

    public string Model { get; set; } = "gpt-4o";

    [Range(1, 4096)]
    public int MaxTokens { get; set; } = 1024;
}
