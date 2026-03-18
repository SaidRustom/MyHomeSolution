using MediatR;
using MyHomeSolution.Application.Features.Budgets.Common;

namespace MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetTree;

public sealed record GetBudgetTreeQuery : IRequest<IReadOnlyList<BudgetTreeNodeDto>>;
