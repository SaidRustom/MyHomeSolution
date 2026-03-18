using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Budgets.Common;

public sealed record BudgetBriefDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal Amount { get; init; }
    public required string Currency { get; init; }
    public BudgetCategory Category { get; init; }
    public BudgetPeriod Period { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public bool IsRecurring { get; init; }
    public bool IsShared { get; init; }
    public Guid? ParentBudgetId { get; init; }
    public string? ParentBudgetName { get; init; }
    public int ChildBudgetCount { get; init; }

    /// <summary>Total spent in the current period.</summary>
    public decimal CurrentPeriodSpent { get; init; }

    /// <summary>Remaining in the current period.</summary>
    public decimal CurrentPeriodRemaining { get; init; }

    /// <summary>Percentage of current period spent (0-100+).</summary>
    public decimal CurrentPeriodPercentUsed { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
