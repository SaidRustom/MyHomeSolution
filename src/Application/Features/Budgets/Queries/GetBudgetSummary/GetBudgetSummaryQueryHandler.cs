using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetSummary;

public sealed class GetBudgetSummaryQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetBudgetSummaryQuery, BudgetSummaryDto>
{
    public async Task<BudgetSummaryDto> Handle(
        GetBudgetSummaryQuery request, CancellationToken cancellationToken)
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

        var budgetQuery = dbContext.Budgets
            .AsNoTracking()
            .Include(b => b.Occurrences)
            .Where(b => !b.IsDeleted)
            .Where(b => b.CreatedBy == userId || sharedBudgetIds.Contains(b.Id));

        if (request.Category.HasValue)
            budgetQuery = budgetQuery.Where(b => b.Category == request.Category.Value);

        if (request.Period.HasValue)
            budgetQuery = budgetQuery.Where(b => b.Period == request.Period.Value);

        var budgets = await budgetQuery.ToListAsync(cancellationToken);

        var fromDate = request.FromDate ?? now.AddMonths(-1);
        var toDate = request.ToDate ?? now;

        // Get current/relevant occurrences for each budget
        var budgetStatuses = budgets.Select(b =>
        {
            var relevantOccurrences = b.Occurrences
                .Where(o => o.PeriodStart <= toDate && o.PeriodEnd >= fromDate)
                .ToList();

            var budgeted = relevantOccurrences.Sum(o => o.AllocatedAmount + o.CarryoverAmount);
            var spent = relevantOccurrences.Sum(o => o.SpentAmount);
            var remaining = budgeted - spent;
            var percentUsed = budgeted > 0 ? Math.Round(spent / budgeted * 100, 2) : 0;

            var status = percentUsed switch
            {
                >= 100 => "over",
                >= 80 => "warning",
                >= 0 => "on-track",
                _ => "under"
            };

            return new BudgetStatusDto
            {
                BudgetId = b.Id,
                BudgetName = b.Name,
                Category = b.Category,
                Budgeted = budgeted,
                Spent = spent,
                Remaining = remaining,
                PercentUsed = percentUsed,
                Status = status
            };
        }).ToList();

        // Aggregate by category
        var byCategory = budgetStatuses
            .GroupBy(s => s.Category)
            .Select(g => new BudgetCategorySpendingDto
            {
                Category = g.Key,
                Budgeted = g.Sum(s => s.Budgeted),
                Spent = g.Sum(s => s.Spent),
                Remaining = g.Sum(s => s.Remaining),
                PercentUsed = g.Sum(s => s.Budgeted) > 0
                    ? Math.Round(g.Sum(s => s.Spent) / g.Sum(s => s.Budgeted) * 100, 2)
                    : 0,
                BudgetCount = g.Count()
            })
            .OrderByDescending(c => c.Spent)
            .ToList();

        // Aggregate by period
        var allOccurrences = budgets
            .SelectMany(b => b.Occurrences)
            .Where(o => o.PeriodStart <= toDate && o.PeriodEnd >= fromDate)
            .GroupBy(o => new { o.PeriodStart, o.PeriodEnd })
            .Select(g => new BudgetPeriodSpendingDto
            {
                PeriodStart = g.Key.PeriodStart,
                PeriodEnd = g.Key.PeriodEnd,
                PeriodLabel = FormatPeriodLabel(g.Key.PeriodStart, g.Key.PeriodEnd),
                Budgeted = g.Sum(o => o.AllocatedAmount + o.CarryoverAmount),
                Spent = g.Sum(o => o.SpentAmount),
                Remaining = g.Sum(o => o.AllocatedAmount + o.CarryoverAmount - o.SpentAmount),
                PercentUsed = g.Sum(o => o.AllocatedAmount + o.CarryoverAmount) > 0
                    ? Math.Round(g.Sum(o => o.SpentAmount) / g.Sum(o => o.AllocatedAmount + o.CarryoverAmount) * 100, 2)
                    : 0
            })
            .OrderBy(p => p.PeriodStart)
            .ToList();

        var totalBudgeted = budgetStatuses.Sum(s => s.Budgeted);
        var totalSpent = budgetStatuses.Sum(s => s.Spent);

        return new BudgetSummaryDto
        {
            TotalBudgeted = totalBudgeted,
            TotalSpent = totalSpent,
            TotalRemaining = totalBudgeted - totalSpent,
            OverallPercentUsed = totalBudgeted > 0
                ? Math.Round(totalSpent / totalBudgeted * 100, 2) : 0,
            TotalBudgets = budgets.Count,
            OverBudgetCount = budgetStatuses.Count(s => s.Status == "over"),
            UnderBudgetCount = budgetStatuses.Count(s => s.Status == "on-track" || s.Status == "under"),
            OnTrackCount = budgetStatuses.Count(s => s.Status == "on-track"),
            ByCategory = byCategory,
            ByPeriod = allOccurrences,
            BudgetStatuses = budgetStatuses
                .OrderByDescending(s => s.PercentUsed)
                .ToList()
        };
    }

    private static string FormatPeriodLabel(DateTimeOffset start, DateTimeOffset end)
    {
        var days = (end - start).TotalDays;
        if (days <= 8)
            return $"Week of {start:MMM dd}";
        if (days <= 32)
            return start.ToString("MMM yyyy");
        return $"{start:MMM yyyy} – {end:MMM yyyy}";
    }
}
