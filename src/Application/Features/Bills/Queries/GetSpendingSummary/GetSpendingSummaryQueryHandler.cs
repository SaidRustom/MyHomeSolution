using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Bills.Queries.GetSpendingSummary;

public sealed class GetSpendingSummaryQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService)
    : IRequestHandler<GetSpendingSummaryQuery, SpendingSummaryDto>
{
    public async Task<SpendingSummaryDto> Handle(
        GetSpendingSummaryQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var splitBillIds = await dbContext.BillSplits
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => s.BillId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var allBills = await dbContext.Bills
            .AsNoTracking()
            .Include(b => b.Splits)
            .Where(b => !b.IsDeleted)
            .Where(b => b.CreatedBy == userId || splitBillIds.Contains(b.Id))
            .ToListAsync(cancellationToken);

        var bills = allBills.AsEnumerable();

        if (request.FromDate.HasValue)
            bills = bills.Where(b => b.BillDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            bills = bills.Where(b => b.BillDate <= request.ToDate.Value);

        var billList = bills.ToList();

        var totalSpent = billList.Where(b => b.PaidByUserId == userId).Sum(b => b.Amount);

        var totalOwed = billList
            .Where(b => b.PaidByUserId == userId)
            .SelectMany(b => b.Splits)
            .Where(s => s.UserId != userId && s.Status != SplitStatus.Paid)
            .Sum(s => s.Amount);

        var totalOwing = billList
            .Where(b => b.PaidByUserId != userId)
            .SelectMany(b => b.Splits)
            .Where(s => s.UserId == userId && s.Status != SplitStatus.Paid)
            .Sum(s => s.Amount);

        var byCategory = billList
            .GroupBy(b => b.Category)
            .Select(g => new CategorySpendingDto
            {
                Category = g.Key,
                TotalAmount = g.Sum(b => b.Splits.Where(s => s.UserId == userId).Sum(s => s.Amount)),
                BillCount = g.Count()
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        var allUserIds = billList.SelectMany(b => b.Splits).Select(s => s.UserId)
            .Union(billList.Select(b => b.PaidByUserId))
            .Where(id => id != userId)
            .Distinct()
            .ToList();

        var nameMap = await identityService.GetUserFullNamesByIdsAsync(allUserIds, cancellationToken);

        var byUser = allUserIds.Select(otherUserId =>
        {
            var paidByMe = billList
                .Where(b => b.PaidByUserId == userId)
                .SelectMany(b => b.Splits)
                .Where(s => s.UserId == otherUserId && s.Status != SplitStatus.Paid)
                .Sum(s => s.Amount);

            var paidByThem = billList
                .Where(b => b.PaidByUserId == otherUserId)
                .SelectMany(b => b.Splits)
                .Where(s => s.UserId == userId && s.Status != SplitStatus.Paid)
                .Sum(s => s.Amount);

            return new UserSpendingDto
            {
                UserId = otherUserId,
                UserFullName = nameMap.GetValueOrDefault(otherUserId),
                TotalPaid = billList.Where(b => b.PaidByUserId == otherUserId).Sum(b => b.Amount),
                TotalOwed = paidByMe,
                TotalOwing = paidByThem,
                NetBalance = paidByMe - paidByThem
            };
        })
        .OrderByDescending(u => Math.Abs(u.NetBalance))
        .ToList();

        return new SpendingSummaryDto
        {
            TotalSpent = totalSpent,
            TotalOwed = totalOwed,
            TotalOwing = totalOwing,
            NetBalance = totalOwed - totalOwing,
            ByCategory = byCategory,
            ByUser = byUser
        };
    }
}
