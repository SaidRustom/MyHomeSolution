using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Common;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetOccurrences;

public sealed class GetBudgetOccurrencesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetBudgetOccurrencesQuery, IReadOnlyList<BudgetOccurrenceDto>>
{
    public async Task<IReadOnlyList<BudgetOccurrenceDto>> Handle(
        GetBudgetOccurrencesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var budget = await dbContext.Budgets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BudgetId && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Budget), request.BudgetId);

        var isOwner = budget.CreatedBy == userId;
        var hasShareAccess = await dbContext.EntityShares
            .AnyAsync(s => s.EntityType == EntityTypes.Budget
                && s.EntityId == budget.Id
                && s.SharedWithUserId == userId
                && !s.IsDeleted, cancellationToken);

        if (!isOwner && !hasShareAccess)
            throw new ForbiddenAccessException();

        var query = dbContext.BudgetOccurrences
            .AsNoTracking()
            .Include(o => o.OutgoingTransfers)
            .Include(o => o.IncomingTransfers)
            .Where(o => o.BudgetId == request.BudgetId);

        if (request.FromDate.HasValue)
            query = query.Where(o => o.PeriodEnd >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(o => o.PeriodStart <= request.ToDate.Value);

        var occurrences = await query
            .OrderByDescending(o => o.PeriodStart)
            .ToListAsync(cancellationToken);

        // Resolve transfer budget names
        var allTransferOccurrenceIds = occurrences
            .SelectMany(o => o.OutgoingTransfers.Select(t => t.DestinationOccurrenceId))
            .Concat(occurrences.SelectMany(o => o.IncomingTransfers.Select(t => t.SourceOccurrenceId)))
            .Distinct()
            .ToList();

        var occurrenceBudgetMap = await dbContext.BudgetOccurrences
            .AsNoTracking()
            .Include(o => o.Budget)
            .Where(o => allTransferOccurrenceIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.Budget.Name, cancellationToken);

        return occurrences.Select(o =>
        {
            var allTransfers = o.OutgoingTransfers
                .Select(t => new BudgetTransferDto
                {
                    Id = t.Id,
                    SourceOccurrenceId = t.SourceOccurrenceId,
                    SourceBudgetName = budget.Name,
                    DestinationOccurrenceId = t.DestinationOccurrenceId,
                    DestinationBudgetName = occurrenceBudgetMap.GetValueOrDefault(t.DestinationOccurrenceId),
                    Amount = t.Amount,
                    Reason = t.Reason,
                    CreatedAt = t.CreatedAt
                })
                .Concat(o.IncomingTransfers.Select(t => new BudgetTransferDto
                {
                    Id = t.Id,
                    SourceOccurrenceId = t.SourceOccurrenceId,
                    SourceBudgetName = occurrenceBudgetMap.GetValueOrDefault(t.SourceOccurrenceId),
                    DestinationOccurrenceId = t.DestinationOccurrenceId,
                    DestinationBudgetName = budget.Name,
                    Amount = t.Amount,
                    Reason = t.Reason,
                    CreatedAt = t.CreatedAt
                }))
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            var totalAllocated = o.AllocatedAmount + o.CarryoverAmount;
            var remaining = totalAllocated - o.SpentAmount;
            var pct = totalAllocated > 0
                ? Math.Round(o.SpentAmount / totalAllocated * 100, 2)
                : 0;

            return new BudgetOccurrenceDto
            {
                Id = o.Id,
                PeriodStart = o.PeriodStart,
                PeriodEnd = o.PeriodEnd,
                AllocatedAmount = o.AllocatedAmount,
                SpentAmount = o.SpentAmount,
                CarryoverAmount = o.CarryoverAmount,
                RemainingAmount = remaining,
                PercentUsed = pct,
                Notes = o.Notes,
                Transfers = allTransfers
            };
        }).ToList();
    }
}
