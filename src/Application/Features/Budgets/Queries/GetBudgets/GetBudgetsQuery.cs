using MediatR;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Budgets.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Budgets.Queries.GetBudgets;

public sealed record GetBudgetsQuery : IRequest<PaginatedList<BudgetBriefDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public BudgetCategory? Category { get; init; }
    public BudgetPeriod? Period { get; init; }
    public string? SearchTerm { get; init; }
    public bool? IsRecurring { get; init; }
    public bool? IsOverBudget { get; init; }
    public Guid? ParentBudgetId { get; init; }
    public bool? RootOnly { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
}
