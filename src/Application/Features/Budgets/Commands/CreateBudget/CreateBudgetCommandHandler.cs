using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Budgets.Commands.CreateBudget;

public sealed class CreateBudgetCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CreateBudgetCommand, Guid>
{
    public async Task<Guid> Handle(CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        if (request.ParentBudgetId.HasValue)
        {
            var parentExists = await dbContext.Budgets
                .AnyAsync(b => b.Id == request.ParentBudgetId.Value && !b.IsDeleted, cancellationToken);
            if (!parentExists)
                throw new NotFoundException(nameof(Budget), request.ParentBudgetId.Value);
        }

        var budget = new Budget
        {
            Name = request.Name,
            Description = request.Description,
            Amount = request.Amount,
            Currency = request.Currency,
            Category = request.Category,
            Period = request.Period,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsRecurring = request.IsRecurring,
            ParentBudgetId = request.ParentBudgetId
        };

        // Generate the first occurrence
        var (periodStart, periodEnd) = CalculatePeriodBounds(request.StartDate, request.Period);

        budget.Occurrences.Add(new BudgetOccurrence
        {
            BudgetId = budget.Id,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            AllocatedAmount = request.Amount,
            CarryoverAmount = 0
        });

        // If this is a child of a recurring parent, create a transfer from the parent's current occurrence
        if (request.ParentBudgetId.HasValue)
        {
            var parentOccurrence = await dbContext.BudgetOccurrences
                .Where(o => o.BudgetId == request.ParentBudgetId.Value)
                .Where(o => o.PeriodStart <= dateTimeProvider.UtcNow && o.PeriodEnd >= dateTimeProvider.UtcNow)
                .FirstOrDefaultAsync(cancellationToken);

            if (parentOccurrence is not null)
            {
                var childOccurrence = budget.Occurrences.First();
                var transfer = new BudgetTransfer
                {
                    SourceOccurrenceId = parentOccurrence.Id,
                    DestinationOccurrenceId = childOccurrence.Id,
                    Amount = request.Amount,
                    Reason = $"Initial allocation from parent budget"
                };

                parentOccurrence.AllocatedAmount -= request.Amount;
                dbContext.BudgetTransfers.Add(transfer);
            }
        }

        dbContext.Budgets.Add(budget);
        await dbContext.SaveChangesAsync(cancellationToken);

        return budget.Id;
    }

    internal static (DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd) CalculatePeriodBounds(
        DateTimeOffset startDate, BudgetPeriod period)
    {
        return period switch
        {
            BudgetPeriod.Weekly => (startDate, startDate.AddDays(7).AddTicks(-1)),
            BudgetPeriod.Monthly => (startDate, startDate.AddMonths(1).AddTicks(-1)),
            BudgetPeriod.Annually => (startDate, startDate.AddYears(1).AddTicks(-1)),
            _ => (startDate, startDate.AddMonths(1).AddTicks(-1))
        };
    }
}
