namespace BlazorUI.Models.Budgets;

public sealed record BudgetTrendsDto
{
    public IReadOnlyList<BudgetTrendPeriodDto> Periods { get; init; } = [];
    public decimal AverageSpentPerPeriod { get; init; }
    public decimal AverageBudgetedPerPeriod { get; init; }
    public decimal AverageUtilization { get; init; }
    public string TrendDirection { get; init; } = "stable";
}

public sealed record BudgetTrendPeriodDto
{
    public DateTimeOffset PeriodStart { get; init; }
    public DateTimeOffset PeriodEnd { get; init; }
    public string PeriodLabel { get; init; } = default!;
    public decimal Budgeted { get; init; }
    public decimal Spent { get; init; }
    public decimal Remaining { get; init; }
    public decimal PercentUsed { get; init; }
    public decimal TransfersIn { get; init; }
    public decimal TransfersOut { get; init; }
}
