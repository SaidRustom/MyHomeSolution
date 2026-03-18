using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Common;

namespace MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetTrends;

public sealed class GetBudgetTrendsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetBudgetTrendsQuery, BudgetTrendsDto>
{
    public async Task<BudgetTrendsDto> Handle(
        GetBudgetTrendsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var asOf = request.AsOfDate ?? dateTimeProvider.UtcNow;

        var sharedBudgetIds = await dbContext.EntityShares
            .AsNoTracking()
            .Where(s => s.EntityType == EntityTypes.Budget
                && s.SharedWithUserId == userId
                && !s.IsDeleted)
            .Select(s => s.EntityId)
            .ToListAsync(cancellationToken);

        var occurrenceQuery = dbContext.BudgetOccurrences
            .AsNoTracking()
            .Include(o => o.Budget)
            .Include(o => o.OutgoingTransfers)
            .Include(o => o.IncomingTransfers)
            .Where(o => !o.Budget.IsDeleted)
            .Where(o => o.Budget.CreatedBy == userId || sharedBudgetIds.Contains(o.BudgetId))
            .Where(o => o.PeriodEnd <= asOf);

        if (request.BudgetId.HasValue)
            occurrenceQuery = occurrenceQuery.Where(o => o.BudgetId == request.BudgetId.Value);

        var allOccurrences = await occurrenceQuery
            .OrderByDescending(o => o.PeriodStart)
            .ToListAsync(cancellationToken);

        // Group by period and take the last N periods
        var periodGroups = allOccurrences
            .GroupBy(o => new { o.PeriodStart, o.PeriodEnd })
            .OrderByDescending(g => g.Key.PeriodStart)
            .Take(request.Periods)
            .OrderBy(g => g.Key.PeriodStart)
            .ToList();

        var periods = periodGroups.Select(g =>
        {
            var budgeted = g.Sum(o => o.AllocatedAmount + o.CarryoverAmount);
            var spent = g.Sum(o => o.SpentAmount);
            var remaining = budgeted - spent;
            var pct = budgeted > 0 ? Math.Round(spent / budgeted * 100, 2) : 0;
            var transfersIn = g.Sum(o => o.IncomingTransfers.Sum(t => t.Amount));
            var transfersOut = g.Sum(o => o.OutgoingTransfers.Sum(t => t.Amount));

            return new BudgetTrendPeriodDto
            {
                PeriodStart = g.Key.PeriodStart,
                PeriodEnd = g.Key.PeriodEnd,
                PeriodLabel = FormatPeriodLabel(g.Key.PeriodStart, g.Key.PeriodEnd),
                Budgeted = budgeted,
                Spent = spent,
                Remaining = remaining,
                PercentUsed = pct,
                TransfersIn = transfersIn,
                TransfersOut = transfersOut
            };
        }).ToList();

        var avgSpent = periods.Count > 0 ? Math.Round(periods.Average(p => p.Spent), 2) : 0;
        var avgBudgeted = periods.Count > 0 ? Math.Round(periods.Average(p => p.Budgeted), 2) : 0;
        var avgUtilization = periods.Count > 0 ? Math.Round(periods.Average(p => p.PercentUsed), 2) : 0;

        // Determine trend direction
        var trendDirection = "stable";
        if (periods.Count >= 2)
        {
            var firstHalf = periods.Take(periods.Count / 2).Average(p => p.PercentUsed);
            var secondHalf = periods.Skip(periods.Count / 2).Average(p => p.PercentUsed);
            var diff = secondHalf - firstHalf;
            trendDirection = diff switch
            {
                > 5 => "increasing",
                < -5 => "decreasing",
                _ => "stable"
            };
        }

        return new BudgetTrendsDto
        {
            Periods = periods,
            AverageSpentPerPeriod = avgSpent,
            AverageBudgetedPerPeriod = avgBudgeted,
            AverageUtilization = avgUtilization,
            TrendDirection = trendDirection
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
