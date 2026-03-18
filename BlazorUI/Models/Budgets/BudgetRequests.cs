namespace BlazorUI.Models.Budgets;

public sealed record CreateBudgetRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "$";
    public BudgetCategory Category { get; init; }
    public BudgetPeriod Period { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public bool IsRecurring { get; init; }
    public Guid? ParentBudgetId { get; init; }
}

public sealed record UpdateBudgetRequest
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "$";
    public BudgetCategory Category { get; init; }
    public BudgetPeriod Period { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public bool IsRecurring { get; init; }
    public Guid? ParentBudgetId { get; init; }
}

public sealed record EditOccurrenceAmountRequest
{
    public Guid OccurrenceId { get; init; }
    public decimal NewAmount { get; init; }
    public string? Notes { get; init; }
    public Guid? TransferOccurrenceId { get; init; }
    public string? TransferReason { get; init; }
}

public sealed record TransferFundsRequest
{
    public Guid SourceOccurrenceId { get; init; }
    public Guid DestinationOccurrenceId { get; init; }
    public decimal Amount { get; init; }
    public string? Reason { get; init; }
}
