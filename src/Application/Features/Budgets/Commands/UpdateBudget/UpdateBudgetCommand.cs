using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Budgets.Commands.UpdateBudget;

public sealed record UpdateBudgetCommand : IRequest, IRequireEditAccess
{
    public Guid Id { get; init; }
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

    public string ResourceType => EntityTypes.Budget;
    public Guid ResourceId => Id;
}
