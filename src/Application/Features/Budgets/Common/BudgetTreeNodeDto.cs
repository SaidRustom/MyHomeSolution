using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Budgets.Common;

public sealed record BudgetTreeNodeDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal Amount { get; init; }
    public required string Currency { get; init; }
    public BudgetCategory Category { get; init; }
    public BudgetPeriod Period { get; init; }
    public bool IsRecurring { get; init; }
    public decimal CurrentPeriodAllocated { get; init; }
    public decimal CurrentPeriodSpent { get; init; }
    public decimal CurrentPeriodRemaining { get; init; }
    public decimal PercentUsed { get; init; }
    public int TotalTransfersIn { get; init; }
    public int TotalTransfersOut { get; init; }
    public decimal NetTransferAmount { get; init; }
    public IReadOnlyList<BudgetTreeNodeDto> Children { get; init; } = [];
}
