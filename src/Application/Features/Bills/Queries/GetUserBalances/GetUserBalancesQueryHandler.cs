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

        // Splits where other users owe the current user (current user paid the bill)
        var owedToMeQuery = dbContext.BillSplits
            .AsNoTracking()
            .Include(s => s.Bill)
            .Where(s => !s.Bill.IsDeleted
                && s.OwedToUserId == userId
                && s.UserId != userId
                && s.Status == SplitStatus.Paid);

        // Splits where current user owes someone else (someone else paid and current user's split has OwedToUserId set)
        var iOweQuery = dbContext.BillSplits
            .AsNoTracking()
            .Include(s => s.Bill)
            .Where(s => !s.Bill.IsDeleted
                && s.UserId == userId
                && s.OwedToUserId != null
                && s.OwedToUserId != userId
                && s.Status == SplitStatus.Paid);

        // Also include legacy unpaid splits for backward compatibility
        var legacyOwedToMe = dbContext.Bills
            .AsNoTracking()
            .Where(b => !b.IsDeleted && b.PaidByUserId == userId)
            .SelectMany(b => b.Splits)
            .Where(s => s.UserId != userId && s.Status == SplitStatus.Unpaid && s.OwedToUserId == null);

        var legacyIOwe = dbContext.BillSplits
            .AsNoTracking()
            .Include(s => s.Bill)
            .Where(s => s.UserId == userId
                && s.Bill.PaidByUserId != userId
                && !s.Bill.IsDeleted
                && s.Status == SplitStatus.Unpaid
                && s.OwedToUserId == null);

        if (!string.IsNullOrWhiteSpace(request.CounterpartyUserId))
        {
            owedToMeQuery = owedToMeQuery.Where(s => s.UserId == request.CounterpartyUserId);
            iOweQuery = iOweQuery.Where(s => s.OwedToUserId == request.CounterpartyUserId);
            legacyOwedToMe = legacyOwedToMe.Where(s => s.UserId == request.CounterpartyUserId);
            legacyIOwe = legacyIOwe.Where(s => s.Bill.PaidByUserId == request.CounterpartyUserId);
        }

        // Owed to me: splits where OwedToUserId == me, grouped by the debtor (UserId)
        var owedToMe = await owedToMeQuery
            .GroupBy(s => s.UserId)
            .Select(g => new { CounterpartyId = g.Key, Total = g.Sum(s => s.Amount) })
            .ToListAsync(cancellationToken);

        // Legacy owed to me (unpaid splits from bills I paid)
        var legacyOwed = await legacyOwedToMe
            .GroupBy(s => s.UserId)
            .Select(g => new { CounterpartyId = g.Key, Total = g.Sum(s => s.Amount) })
            .ToListAsync(cancellationToken);

        // I owe: my splits with OwedToUserId != me, grouped by who I owe
        var iOwe = await iOweQuery
            .GroupBy(s => s.OwedToUserId!)
            .Select(g => new { CounterpartyId = g.Key, Total = g.Sum(s => s.Amount) })
            .ToListAsync(cancellationToken);

        // Legacy I owe (unpaid splits from bills others paid)
        var legacyOwe = await legacyIOwe
            .GroupBy(s => s.Bill.PaidByUserId)
            .Select(g => new { CounterpartyId = g.Key, Total = g.Sum(s => s.Amount) })
            .ToListAsync(cancellationToken);

        // Merge results
        var mergedOwedToMe = owedToMe.Concat(legacyOwed)
            .GroupBy(x => x.CounterpartyId)
            .Select(g => new { CounterpartyId = g.Key, Total = g.Sum(x => x.Total) })
            .ToList();

        var mergedIOwe = iOwe.Concat(legacyOwe)
            .GroupBy(x => x.CounterpartyId)
            .Select(g => new { CounterpartyId = g.Key, Total = g.Sum(x => x.Total) })
            .ToList();

        var counterpartyIds = mergedOwedToMe.Select(o => o.CounterpartyId)
            .Union(mergedIOwe.Select(o => o.CounterpartyId))
            .Distinct()
            .ToList();

        var nameMap = await identityService.GetUserFullNamesByIdsAsync(counterpartyIds, cancellationToken);

        var result = counterpartyIds.Select(cpId =>
        {
            var owed = mergedOwedToMe.FirstOrDefault(o => o.CounterpartyId == cpId)?.Total ?? 0m;
            var owing = mergedIOwe.FirstOrDefault(o => o.CounterpartyId == cpId)?.Total ?? 0m;

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
