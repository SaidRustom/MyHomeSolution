using MediatR;
using MyHomeSolution.Application.Features.Budgets.Common;

namespace MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetOccurrences;

public sealed record GetBudgetOccurrencesQuery : IRequest<IReadOnlyList<BudgetOccurrenceDto>>
{
    public Guid BudgetId { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
}
