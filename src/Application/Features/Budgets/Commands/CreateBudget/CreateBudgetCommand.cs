using MediatR;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Budgets.Commands.CreateBudget;

public sealed record CreateBudgetCommand : IRequest<Guid>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "CAD";
    public BudgetCategory Category { get; init; }
    public BudgetPeriod Period { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public bool IsRecurring { get; init; }
    public Guid? ParentBudgetId { get; init; }
}
