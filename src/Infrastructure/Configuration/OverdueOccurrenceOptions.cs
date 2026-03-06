using System.ComponentModel.DataAnnotations;

namespace MyHomeSolution.Infrastructure.Configuration;

public sealed class OverdueOccurrenceOptions
{
    public const string SectionName = "OverdueOccurrence";

    [Range(1, 60)]
    public int IntervalMinutes { get; set; } = 2;

    [Range(0, 300)]
    public int StartupDelaySeconds { get; set; } = 15;
}
