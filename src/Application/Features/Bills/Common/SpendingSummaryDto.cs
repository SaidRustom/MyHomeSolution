using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Bills.Common;

public sealed record SpendingSummaryDto
{
    public decimal TotalSpent { get; init; }
    public decimal TotalOwed { get; init; }
    public decimal TotalOwing { get; init; }
    public decimal NetBalance { get; init; }
    public IReadOnlyList<CategorySpendingDto> ByCategory { get; init; } = [];
    public IReadOnlyList<UserSpendingDto> ByUser { get; init; } = [];
}

public sealed record CategorySpendingDto
{
    public BillCategory Category { get; init; }
    public decimal TotalAmount { get; init; }
    public int BillCount { get; init; }
}

public sealed record UserSpendingDto
{
    public required string UserId { get; init; }
    public string? UserFullName { get; init; }
    public decimal TotalPaid { get; init; }
    public decimal TotalOwed { get; init; }
    public decimal TotalOwing { get; init; }
    public decimal NetBalance { get; init; }
}
