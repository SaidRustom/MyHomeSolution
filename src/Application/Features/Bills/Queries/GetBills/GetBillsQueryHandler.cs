using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Bills.Common;

namespace MyHomeSolution.Application.Features.Bills.Queries.GetBills;

public sealed class GetBillsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService)
    : IRequestHandler<GetBillsQuery, PaginatedList<BillBriefDto>>
{
    public async Task<PaginatedList<BillBriefDto>> Handle(
        GetBillsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var splitBillIds = dbContext.BillSplits
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => s.BillId);

        var query = dbContext.Bills
            .AsNoTracking()
            .Where(b => !b.IsDeleted)
            .Where(b => b.CreatedBy == userId || splitBillIds.Contains(b.Id));

        if (request.Category.HasValue)
            query = query.Where(b => b.Category == request.Category.Value);

        if (!string.IsNullOrWhiteSpace(request.PaidByUserId))
            query = query.Where(b => b.PaidByUserId == request.PaidByUserId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(b =>
                b.Title.Contains(request.SearchTerm) ||
                (b.Description != null && b.Description.Contains(request.SearchTerm)));

        if (request.FromDate.HasValue)
            query = query.Where(b => b.BillDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(b => b.BillDate <= request.ToDate.Value);

        var projected = query
            .OrderByDescending(b => b.Id)
            .Select(b => new BillBriefDto
            {
                Id = b.Id,
                Title = b.Title,
                Amount = b.Amount,
                Currency = b.Currency,
                Category = b.Category,
                BillDate = b.BillDate,
                PaidByUserId = b.PaidByUserId,
                HasReceipt = b.ReceiptUrl != null,
                SplitCount = b.Splits.Count,
                CreatedAt = b.CreatedAt
            });

        var page = await PaginatedList<BillBriefDto>.CreateAsync(
            projected, request.PageNumber, request.PageSize, cancellationToken);

        var payerIds = page.Items.Select(b => b.PaidByUserId).Distinct();
        var nameMap = await identityService.GetUserFullNamesByIdsAsync(payerIds, cancellationToken);

        var enriched = page.Items.Select(b => b with
        {
            PaidByUserFullName = nameMap.GetValueOrDefault(b.PaidByUserId)
        }).ToList();

        return new PaginatedList<BillBriefDto>(enriched, page.TotalCount, request.PageNumber, request.PageSize);
    }
}
