using MediatR;
using MyHomeSolution.Application.Features.Bills.Common;

namespace MyHomeSolution.Application.Features.Bills.Queries.GetUserBalances;

public sealed record GetUserBalancesQuery : IRequest<IReadOnlyList<UserBalanceDto>>
{
    public string? CounterpartyUserId { get; init; }
}
