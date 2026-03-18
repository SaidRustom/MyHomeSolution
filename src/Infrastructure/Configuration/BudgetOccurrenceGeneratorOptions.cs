using System.ComponentModel.DataAnnotations;

namespace MyHomeSolution.Infrastructure.Configuration;

public sealed class BudgetOccurrenceGeneratorOptions
{
    public const string SectionName = "BudgetOccurrenceGenerator";

    [Range(1, 1440)]
    public int IntervalMinutes { get; set; } = 5;

    [Range(0, 300)]
    public int StartupDelaySeconds { get; set; } = 45;
}
