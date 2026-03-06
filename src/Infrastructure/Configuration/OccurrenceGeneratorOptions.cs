using System.ComponentModel.DataAnnotations;

namespace MyHomeSolution.Infrastructure.Configuration;

public sealed class OccurrenceGeneratorOptions
{
    public const string SectionName = "OccurrenceGenerator";

    [Range(1, 60)]
    public int IntervalMinutes { get; set; } = 15;

    [Range(1, 50)]
    public int RequiredFutureOccurrences { get; set; } = 5;

    [Range(0, 300)]
    public int StartupDelaySeconds { get; set; } = 30;
}
