using MediatR;
using MyHomeSolution.Application.Features.Budgets.Common;

namespace MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetById;

public sealed record GetBudgetByIdQuery(Guid Id) : IRequest<BudgetDetailDto>;
