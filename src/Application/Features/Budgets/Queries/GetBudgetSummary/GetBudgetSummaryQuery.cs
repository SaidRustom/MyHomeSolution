using MediatR;
using MyHomeSolution.Application.Features.Budgets.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetSummary;

public sealed record GetBudgetSummaryQuery : IRequest<BudgetSummaryDto>
{
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
    public BudgetCategory? Category { get; init; }
    public BudgetPeriod? Period { get; init; }
}
