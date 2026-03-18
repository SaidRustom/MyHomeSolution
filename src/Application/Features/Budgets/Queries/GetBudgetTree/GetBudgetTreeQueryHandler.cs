using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Common;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetTree;

public sealed class GetBudgetTreeQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetBudgetTreeQuery, IReadOnlyList<BudgetTreeNodeDto>>
{
    public async Task<IReadOnlyList<BudgetTreeNodeDto>> Handle(
        GetBudgetTreeQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var now = dateTimeProvider.UtcNow;

        var sharedBudgetIds = await dbContext.EntityShares
            .AsNoTracking()
            .Where(s => s.EntityType == EntityTypes.Budget
                && s.SharedWithUserId == userId
                && !s.IsDeleted)
            .Select(s => s.EntityId)
            .ToListAsync(cancellationToken);

        var allBudgets = await dbContext.Budgets
            .AsNoTracking()
            .Include(b => b.Occurrences)
                .ThenInclude(o => o.OutgoingTransfers)
            .Include(b => b.Occurrences)
                .ThenInclude(o => o.IncomingTransfers)
            .Where(b => !b.IsDeleted)
            .Where(b => b.CreatedBy == userId || sharedBudgetIds.Contains(b.Id))
            .ToListAsync(cancellationToken);

        var budgetMap = allBudgets.ToDictionary(b => b.Id);

        // Build tree starting from root budgets (no parent)
        var rootBudgets = allBudgets
            .Where(b => b.ParentBudgetId == null ||
                        !budgetMap.ContainsKey(b.ParentBudgetId.Value))
            .OrderBy(b => b.Name);

        return rootBudgets
            .Select(b => BuildNode(b, budgetMap, now))
            .ToList();
    }

    private static BudgetTreeNodeDto BuildNode(
        Budget budget,
        Dictionary<Guid, Budget> budgetMap,
        DateTimeOffset now)
    {
        var currentOccurrence = budget.Occurrences
            .FirstOrDefault(o => o.PeriodStart <= now && o.PeriodEnd >= now);

        var allocated = currentOccurrence?.AllocatedAmount ?? budget.Amount;
        var carryover = currentOccurrence?.CarryoverAmount ?? 0;
        var totalAllocated = allocated + carryover;
        var spent = currentOccurrence?.SpentAmount ?? 0;
        var remaining = totalAllocated - spent;
        var pct = totalAllocated > 0
            ? Math.Round(spent / totalAllocated * 100, 2)
            : 0;

        var transfersIn = currentOccurrence?.IncomingTransfers?.Count ?? 0;
        var transfersOut = currentOccurrence?.OutgoingTransfers?.Count ?? 0;
        var netTransfer =
            (currentOccurrence?.IncomingTransfers?.Sum(t => t.Amount) ?? 0)
            - (currentOccurrence?.OutgoingTransfers?.Sum(t => t.Amount) ?? 0);

        var children = budgetMap.Values
            .Where(b => b.ParentBudgetId == budget.Id)
            .OrderBy(b => b.Name)
            .Select(child => BuildNode(child, budgetMap, now))
            .ToList();

        return new BudgetTreeNodeDto
        {
            Id = budget.Id,
            Name = budget.Name,
            Description = budget.Description,
            Amount = budget.Amount,
            Currency = budget.Currency,
            Category = budget.Category,
            Period = budget.Period,
            IsRecurring = budget.IsRecurring,
            CurrentPeriodAllocated = totalAllocated,
            CurrentPeriodSpent = spent,
            CurrentPeriodRemaining = remaining,
            PercentUsed = pct,
            TotalTransfersIn = transfersIn,
            TotalTransfersOut = transfersOut,
            NetTransferAmount = netTransfer,
            Children = children
        };
    }
}
