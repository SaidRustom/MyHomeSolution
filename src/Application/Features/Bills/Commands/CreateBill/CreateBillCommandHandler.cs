using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Bills.Commands.CreateBill;

public sealed class CreateBillCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPublisher publisher)
    : IRequestHandler<CreateBillCommand, Guid>
{
    public async Task<Guid> Handle(CreateBillCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var bill = new Bill
        {
            Title = request.Title,
            Description = request.Description,
            Amount = request.Amount,
            Currency = request.Currency,
            Category = request.Category,
            BillDate = request.BillDate,
            PaidByUserId = userId,
            Notes = request.Notes,
            RelatedEntityId = request.RelatedEntityId,
            RelatedEntityType = request.RelatedEntityType
        };

        var splits = request.Splits ?? new List<BillSplitRequest>();
        if(splits.Count == 0)
            splits.Add(new BillSplitRequest { UserId = userId, Percentage = 100m });

        var hasCustomPercentages = splits.Any(s => s.Percentage.HasValue);
        var equalPercentage = Math.Round(100m / splits.Count, 2);

        foreach (var splitReq in splits)
        {
            var percentage = hasCustomPercentages
                ? splitReq.Percentage!.Value
                : equalPercentage;

            var splitAmount = Math.Round(request.Amount * percentage / 100m, 2);

            bill.Splits.Add(new BillSplit
            {
                BillId = bill.Id,
                UserId = splitReq.UserId,
                Percentage = percentage,
                Amount = splitAmount,
                Status = SplitStatus.Paid
            });
        }

        // Create related item entries for the legacy relation
        if (request.RelatedEntityId.HasValue && !string.IsNullOrEmpty(request.RelatedEntityType))
        {
            bill.RelatedItems.Add(new BillRelatedItem
            {
                BillId = bill.Id,
                RelatedEntityId = request.RelatedEntityId.Value,
                RelatedEntityType = request.RelatedEntityType
            });
        }

        // Resolve budget: explicit BudgetId or ShoppingList/Task default budget
        var budgetId = request.BudgetId;

        if (!budgetId.HasValue
            && request.RelatedEntityId.HasValue
            && string.Equals(request.RelatedEntityType, EntityTypes.ShoppingList, StringComparison.OrdinalIgnoreCase))
        {
            var shoppingList = await dbContext.ShoppingLists
                .AsNoTracking()
                .FirstOrDefaultAsync(sl => sl.Id == request.RelatedEntityId.Value && !sl.IsDeleted, cancellationToken);

            if (shoppingList?.DefaultBudgetId.HasValue == true)
                budgetId = shoppingList.DefaultBudgetId.Value;
        }

        if (!budgetId.HasValue
            && request.RelatedEntityId.HasValue
            && string.Equals(request.RelatedEntityType, EntityTypes.HouseholdTask, StringComparison.OrdinalIgnoreCase))
        {
            var task = await dbContext.HouseholdTasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == request.RelatedEntityId.Value && !t.IsDeleted, cancellationToken);

            if (task?.DefaultBudgetId.HasValue == true)
                budgetId = task.DefaultBudgetId.Value;
        }

        if (budgetId.HasValue)
        {
            var budgetExists = await dbContext.Budgets
                .AnyAsync(b => b.Id == budgetId.Value && !b.IsDeleted, cancellationToken);

            if (budgetExists)
            {
                // Find the active occurrence, or fall back to the most recent
                var activeOccurrence = await dbContext.BudgetOccurrences
                    .Where(o => o.BudgetId == budgetId.Value
                        && o.PeriodStart <= bill.BillDate && o.PeriodEnd >= bill.BillDate)
                    .FirstOrDefaultAsync(cancellationToken);

                var usedFallback = false;
                if (activeOccurrence is null)
                {
                    activeOccurrence = await dbContext.BudgetOccurrences
                        .Where(o => o.BudgetId == budgetId.Value)
                        .OrderByDescending(o => o.PeriodStart)
                        .FirstOrDefaultAsync(cancellationToken);
                    usedFallback = activeOccurrence is not null;
                }

                if (activeOccurrence is not null)
                {
                    bill.BudgetLink = new BillBudgetLink
                    {
                        BillId = bill.Id,
                        BudgetId = budgetId.Value,
                        BudgetOccurrenceId = activeOccurrence.Id
                    };

                    // Add budget as a related item
                    bill.RelatedItems.Add(new BillRelatedItem
                    {
                        BillId = bill.Id,
                        RelatedEntityId = budgetId.Value,
                        RelatedEntityType = EntityTypes.Budget
                    });

                    // Notify user if no active occurrence was found and fallback was used
                    if (usedFallback)
                    {
                        dbContext.Notifications.Add(new Notification
                        {
                            Title = "No Active Budget Period",
                            Description = $"Bill '{bill.Title}' was linked to the most recent budget period because no active period was found for the budget.",
                            Type = NotificationType.BudgetThresholdReached,
                            FromUserId = userId,
                            ToUserId = userId,
                            RelatedEntityId = budgetId.Value,
                            RelatedEntityType = EntityTypes.Budget
                        });
                    }
                }
            }
        }

        dbContext.Bills.Add(bill);
        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new BillCreatedEvent(bill.Id, bill.Title, bill.Amount, userId),
            cancellationToken);

        return bill.Id;
    }
}
