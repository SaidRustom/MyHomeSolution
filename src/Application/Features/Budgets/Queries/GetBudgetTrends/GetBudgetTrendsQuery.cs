using MediatR;
using MyHomeSolution.Application.Features.Budgets.Common;

namespace MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetTrends;

public sealed record GetBudgetTrendsQuery : IRequest<BudgetTrendsDto>
{
    public Guid? BudgetId { get; init; }
    public int Periods { get; init; } = 6;
    public DateTimeOffset? AsOfDate { get; init; }
}
