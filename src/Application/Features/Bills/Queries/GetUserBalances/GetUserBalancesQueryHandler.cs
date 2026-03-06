using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Bills.Queries.GetUserBalances;

public sealed class GetUserBalancesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService)
    : IRequestHandler<GetUserBalancesQuery, IReadOnlyList<UserBalanceDto>>
{
    public async Task<IReadOnlyList<UserBalanceDto>> Handle(
        GetUserBalancesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        // Bills the current user paid where others owe them (unpaid splits)
        var owedToMeQuery = dbContext.Bills
            .AsNoTracking()
            .Where(b => !b.IsDeleted && b.PaidByUserId == userId)
            .SelectMany(b => b.Splits)
            .Where(s => s.UserId != userId && s.Status != SplitStatus.Paid);

        // Bills others paid where current user owes (unpaid splits)
        var iOweQuery = dbContext.BillSplits
            .AsNoTracking()
            .Include(s => s.Bill)
            .Where(s => s.UserId == userId
                && s.Bill.PaidByUserId != userId
                && !s.Bill.IsDeleted
                && s.Status != SplitStatus.Paid);

        if (!string.IsNullOrWhiteSpace(request.CounterpartyUserId))
        {
            owedToMeQuery = owedToMeQuery.Where(s => s.UserId == request.CounterpartyUserId);
            iOweQuery = iOweQuery.Where(s => s.Bill.PaidByUserId == request.CounterpartyUserId);
        }

        var owedToMe = await owedToMeQuery
            .GroupBy(s => s.UserId)
            .Select(g => new { CounterpartyId = g.Key, Total = g.Sum(s => s.Amount) })
            .ToListAsync(cancellationToken);

        var iOwe = await iOweQuery
            .GroupBy(s => s.Bill.PaidByUserId)
            .Select(g => new { CounterpartyId = g.Key, Total = g.Sum(s => s.Amount) })
            .ToListAsync(cancellationToken);

        var counterpartyIds = owedToMe.Select(o => o.CounterpartyId)
            .Union(iOwe.Select(o => o.CounterpartyId))
            .Distinct()
            .ToList();

        var nameMap = await identityService.GetUserFullNamesByIdsAsync(counterpartyIds, cancellationToken);

        var result = counterpartyIds.Select(cpId =>
        {
            var owed = owedToMe.FirstOrDefault(o => o.CounterpartyId == cpId)?.Total ?? 0m;
            var owing = iOwe.FirstOrDefault(o => o.CounterpartyId == cpId)?.Total ?? 0m;

            return new UserBalanceDto
            {
                UserId = userId,
                CounterpartyUserId = cpId,
                CounterpartyFullName = nameMap.GetValueOrDefault(cpId),
                TotalOwed = owed,
                TotalOwing = owing,
                NetBalance = owed - owing
            };
        }).ToList();

        return result;
    }
}
