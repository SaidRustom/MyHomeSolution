using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Budgets;

public sealed record BudgetDetailDto
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
    public Guid? ParentBudgetId { get; init; }
    public string? ParentBudgetName { get; init; }

    public IReadOnlyList<BudgetOccurrenceDto> Occurrences { get; init; } = [];
    public IReadOnlyList<BudgetChildDto> ChildBudgets { get; init; } = [];
    public IReadOnlyList<BudgetBillDto> LinkedBills { get; init; } = [];
    public IReadOnlyList<BudgetTaskDto> LinkedTasks { get; init; } = [];
    public IReadOnlyList<BudgetShoppingListDto> LinkedShoppingLists { get; init; } = [];

    public decimal TotalSpent { get; init; }
    public decimal TotalAllocated { get; init; }
    public decimal TotalRemaining { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedByUserId { get; init; }
    public string? CreatedByFullName { get; init; }
    public DateTimeOffset? LastModifiedAt { get; init; }
}

public sealed record BudgetOccurrenceDto
{
    public Guid Id { get; init; }
    public DateTimeOffset PeriodStart { get; init; }
    public DateTimeOffset PeriodEnd { get; init; }
    public decimal AllocatedAmount { get; init; }
    public decimal SpentAmount { get; init; }
    public decimal CarryoverAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public decimal PercentUsed { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<BudgetTransferDto> Transfers { get; init; } = [];
}

public sealed record BudgetTransferDto
{
    public Guid Id { get; init; }
    public Guid SourceOccurrenceId { get; init; }
    public string? SourceBudgetName { get; init; }
    public Guid DestinationOccurrenceId { get; init; }
    public string? DestinationBudgetName { get; init; }
    public decimal Amount { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record BudgetChildDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public decimal Amount { get; init; }
    public BudgetCategory Category { get; init; }
    public BudgetPeriod Period { get; init; }
    public decimal CurrentPeriodSpent { get; init; }
    public decimal CurrentPeriodRemaining { get; init; }
}

public sealed record BudgetBillDto
{
    public Guid BillId { get; init; }
    public required string BillTitle { get; init; }
    public decimal BillAmount { get; init; }
    public DateTimeOffset BillDate { get; init; }
    public BillCategory BillCategory { get; init; }
}

public sealed record BudgetTaskDto
{
    public Guid TaskId { get; init; }
    public required string TaskTitle { get; init; }
    public bool IsRecurring { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<BudgetBillDto> Bills { get; init; } = [];
}

public sealed record BudgetShoppingListDto
{
    public Guid ShoppingListId { get; init; }
    public required string Title { get; init; }
    public bool IsCompleted { get; init; }
    public IReadOnlyList<BudgetBillDto> Bills { get; init; } = [];
}
