namespace BlazorUI.Models.Budgets;

public sealed record BudgetSummaryDto
{
    public decimal TotalBudgeted { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal TotalRemaining { get; init; }
    public decimal OverallPercentUsed { get; init; }
    public int TotalBudgets { get; init; }
    public int OverBudgetCount { get; init; }
    public int UnderBudgetCount { get; init; }
    public int OnTrackCount { get; init; }
    public IReadOnlyList<BudgetCategorySpendingDto> ByCategory { get; init; } = [];
    public IReadOnlyList<BudgetPeriodSpendingDto> ByPeriod { get; init; } = [];
    public IReadOnlyList<BudgetStatusDto> BudgetStatuses { get; init; } = [];
}

public sealed record BudgetCategorySpendingDto
{
    public BudgetCategory Category { get; init; }
    public decimal Budgeted { get; init; }
    public decimal Spent { get; init; }
    public decimal Remaining { get; init; }
    public decimal PercentUsed { get; init; }
    public int BudgetCount { get; init; }
}

public sealed record BudgetPeriodSpendingDto
{
    public DateTimeOffset PeriodStart { get; init; }
    public DateTimeOffset PeriodEnd { get; init; }
    public string PeriodLabel { get; init; } = default!;
    public decimal Budgeted { get; init; }
    public decimal Spent { get; init; }
    public decimal Remaining { get; init; }
    public decimal PercentUsed { get; init; }
}

public sealed record BudgetStatusDto
{
    public Guid BudgetId { get; init; }
    public required string BudgetName { get; init; }
    public BudgetCategory Category { get; init; }
    public decimal Budgeted { get; init; }
    public decimal Spent { get; init; }
    public decimal Remaining { get; init; }
    public decimal PercentUsed { get; init; }
    public required string Status { get; init; }
}
