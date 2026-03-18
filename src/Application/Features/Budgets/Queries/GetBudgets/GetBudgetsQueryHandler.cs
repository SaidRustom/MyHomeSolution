using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Budgets.Common;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Budgets.Queries.GetBudgets;

public sealed class GetBudgetsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetBudgetsQuery, PaginatedList<BudgetBriefDto>>
{
    public async Task<PaginatedList<BudgetBriefDto>> Handle(
        GetBudgetsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var now = dateTimeProvider.UtcNow;

        // Include budgets owned by user OR shared with user
        var sharedBudgetIds = dbContext.EntityShares
            .AsNoTracking()
            .Where(s => s.EntityType == EntityTypes.Budget
                && s.SharedWithUserId == userId
                && !s.IsDeleted)
            .Select(s => s.EntityId);

        var query = dbContext.Budgets
            .AsNoTracking()
            .Include(b => b.Occurrences)
            .Include(b => b.BillLinks)
                .ThenInclude(l => l.Bill)
            .Include(b => b.ParentBudget)
            .Where(b => !b.IsDeleted)
            .Where(b => b.CreatedBy == userId || sharedBudgetIds.Contains(b.Id));

        // Filters
        if (request.Category.HasValue)
            query = query.Where(b => b.Category == request.Category.Value);

        if (request.Period.HasValue)
            query = query.Where(b => b.Period == request.Period.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(b =>
                b.Name.Contains(request.SearchTerm) ||
                (b.Description != null && b.Description.Contains(request.SearchTerm)));

        if (request.IsRecurring.HasValue)
            query = query.Where(b => b.IsRecurring == request.IsRecurring.Value);

        if (request.ParentBudgetId.HasValue)
            query = query.Where(b => b.ParentBudgetId == request.ParentBudgetId.Value);

        if (request.RootOnly == true)
            query = query.Where(b => b.ParentBudgetId == null);

        // Materialize for in-memory calculations
        var budgetList = await query.ToListAsync(cancellationToken);

        // Check shared status
        var allSharedIds = await sharedBudgetIds.ToListAsync(cancellationToken);

        var projectedList = budgetList.Select(b =>
        {
            var currentOccurrence = b.Occurrences
                .FirstOrDefault(o => o.PeriodStart <= now && o.PeriodEnd >= now);

            var currentSpent = currentOccurrence?.SpentAmount ?? 0;
            var currentAllocated = currentOccurrence?.AllocatedAmount ?? b.Amount;
            var currentRemaining = currentAllocated - currentSpent;
            var percentUsed = currentAllocated > 0
                ? Math.Round(currentSpent / currentAllocated * 100, 2)
                : 0;

            return new BudgetBriefDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                Amount = b.Amount,
                Currency = b.Currency,
                Category = b.Category,
                Period = b.Period,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                IsRecurring = b.IsRecurring,
                IsShared = allSharedIds.Contains(b.Id),
                ParentBudgetId = b.ParentBudgetId,
                ParentBudgetName = b.ParentBudget?.Name,
                ChildBudgetCount = 0, // filled below
                CurrentPeriodSpent = currentSpent,
                CurrentPeriodRemaining = currentRemaining,
                CurrentPeriodPercentUsed = percentUsed,
                CreatedAt = b.CreatedAt
            };
        }).ToList();

        // Fill child budget counts
        var budgetIds = projectedList.Select(b => b.Id).ToHashSet();
        var childCounts = await dbContext.Budgets
            .AsNoTracking()
            .Where(b => !b.IsDeleted && b.ParentBudgetId.HasValue && budgetIds.Contains(b.ParentBudgetId.Value))
            .GroupBy(b => b.ParentBudgetId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var childCountMap = childCounts.ToDictionary(c => c.ParentId, c => c.Count);

        projectedList = projectedList.Select(b => b with
        {
            ChildBudgetCount = childCountMap.GetValueOrDefault(b.Id)
        }).ToList();

        // Filter by over-budget (after calculations)
        if (request.IsOverBudget == true)
            projectedList = projectedList.Where(b => b.CurrentPeriodPercentUsed >= 100).ToList();
        else if (request.IsOverBudget == false)
            projectedList = projectedList.Where(b => b.CurrentPeriodPercentUsed < 100).ToList();

        // Sort
        var sortBy = request.SortBy?.ToLowerInvariant();
        var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        projectedList = sortBy switch
        {
            "name" => descending
                ? projectedList.OrderByDescending(b => b.Name).ToList()
                : projectedList.OrderBy(b => b.Name).ToList(),
            "amount" => descending
                ? projectedList.OrderByDescending(b => b.Amount).ToList()
                : projectedList.OrderBy(b => b.Amount).ToList(),
            "spent" => descending
                ? projectedList.OrderByDescending(b => b.CurrentPeriodSpent).ToList()
                : projectedList.OrderBy(b => b.CurrentPeriodSpent).ToList(),
            "remaining" => descending
                ? projectedList.OrderByDescending(b => b.CurrentPeriodRemaining).ToList()
                : projectedList.OrderBy(b => b.CurrentPeriodRemaining).ToList(),
            "percentused" => descending
                ? projectedList.OrderByDescending(b => b.CurrentPeriodPercentUsed).ToList()
                : projectedList.OrderBy(b => b.CurrentPeriodPercentUsed).ToList(),
            _ => descending
                ? projectedList.OrderBy(b => b.CreatedAt).ToList()
                : projectedList.OrderByDescending(b => b.CreatedAt).ToList()
        };

        // Paginate
        var totalCount = projectedList.Count;
        var items = projectedList
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PaginatedList<BudgetBriefDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
