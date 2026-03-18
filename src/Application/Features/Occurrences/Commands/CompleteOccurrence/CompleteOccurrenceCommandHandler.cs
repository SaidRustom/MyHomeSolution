using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Occurrences.Commands.CompleteOccurrence;

public sealed class CompleteOccurrenceCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IPublisher publisher)
    : IRequestHandler<CompleteOccurrenceCommand>
{
    public async Task Handle(CompleteOccurrenceCommand request, CancellationToken cancellationToken)
    {
        var occurrence = await dbContext.TaskOccurrences
            .Include(o => o.HouseholdTask)
            .FirstOrDefaultAsync(o => o.Id == request.OccurrenceId && !o.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskOccurrence), request.OccurrenceId);

        occurrence.Status = OccurrenceStatus.Completed;
        occurrence.CompletedAt = dateTimeProvider.UtcNow;
        occurrence.CompletedByUserId = currentUserService.UserId;
        occurrence.Notes = request.Notes;

        // Auto-create bill when the task has AutoCreateBill enabled
        if (occurrence.HouseholdTask.AutoCreateBill && occurrence.BillId is null)
        {
            var bill = await CreateBillForOccurrenceAsync(occurrence, cancellationToken);
            occurrence.BillId = bill.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new OccurrenceCompletedEvent(occurrence.Id, occurrence.HouseholdTaskId, currentUserService.UserId),
            cancellationToken);
    }

    private async Task<Bill> CreateBillForOccurrenceAsync(
        TaskOccurrence occurrence, CancellationToken cancellationToken)
    {
        var task = occurrence.HouseholdTask;
        var currentUserId = currentUserService.UserId ?? string.Empty;

        // Determine who pays: explicit default payer, or the user completing the task
        var resolvedPaidByUserId = !string.IsNullOrEmpty(task.DefaultBillPaidByUserId)
            ? task.DefaultBillPaidByUserId
            : currentUserId;

        var bill = new Bill
        {
            Title = task.DefaultBillTitle ?? $"{task.Title} — {occurrence.DueDate:MMM dd, yyyy}",
            Description = $"Auto-created from task '{task.Title}' occurrence on {occurrence.DueDate:MMM dd, yyyy}",
            Amount = task.DefaultBillAmount ?? 0m,
            Currency = task.DefaultBillCurrency ?? "CAD",
            Category = task.DefaultBillCategory ?? BillCategory.General,
            BillDate = dateTimeProvider.UtcNow,
            PaidByUserId = resolvedPaidByUserId,
            RelatedEntityId = task.Id,
            RelatedEntityType = EntityTypes.TaskOccurrence
        };

        // Add task as a related item
        bill.RelatedItems.Add(new BillRelatedItem
        {
            BillId = bill.Id,
            RelatedEntityId = task.Id,
            RelatedEntityType = EntityTypes.HouseholdTask,
            RelatedEntityName = task.Title
        });

        // Link to the task's default budget
        if (task.DefaultBudgetId.HasValue)
        {
            var budgetExists = await dbContext.Budgets
                .AnyAsync(b => b.Id == task.DefaultBudgetId.Value && !b.IsDeleted, cancellationToken);

            if (budgetExists)
            {
                // Find the active occurrence, or fall back to the most recent
                var budgetOccurrence = await dbContext.BudgetOccurrences
                    .Where(o => o.BudgetId == task.DefaultBudgetId.Value
                        && o.PeriodStart <= bill.BillDate && o.PeriodEnd >= bill.BillDate)
                    .FirstOrDefaultAsync(cancellationToken);

                var usedFallback = false;
                if (budgetOccurrence is null)
                {
                    budgetOccurrence = await dbContext.BudgetOccurrences
                        .Where(o => o.BudgetId == task.DefaultBudgetId.Value)
                        .OrderByDescending(o => o.PeriodStart)
                        .FirstOrDefaultAsync(cancellationToken);
                    usedFallback = budgetOccurrence is not null;
                }

                if (budgetOccurrence is not null)
                {
                    bill.BudgetLink = new BillBudgetLink
                    {
                        BillId = bill.Id,
                        BudgetId = task.DefaultBudgetId.Value,
                        BudgetOccurrenceId = budgetOccurrence.Id
                    };

                    bill.RelatedItems.Add(new BillRelatedItem
                    {
                        BillId = bill.Id,
                        RelatedEntityId = task.DefaultBudgetId.Value,
                        RelatedEntityType = EntityTypes.Budget
                    });

                    if (usedFallback && !string.IsNullOrEmpty(currentUserId))
                    {
                        dbContext.Notifications.Add(new Notification
                        {
                            Title = "No Active Budget Period",
                            Description = $"Bill '{bill.Title}' was linked to the most recent budget period because no active period was found for the budget.",
                            Type = NotificationType.BudgetThresholdReached,
                            FromUserId = currentUserId,
                            ToUserId = currentUserId,
                            RelatedEntityId = task.DefaultBudgetId.Value,
                            RelatedEntityType = EntityTypes.Budget
                        });
                    }
                }
            }
        }

        // Gather all users who share this task
        var sharedUserIds = await dbContext.EntityShares
            .AsNoTracking()
            .Where(s => s.EntityType == EntityTypes.HouseholdTask
                && s.EntityId == task.Id
                && !s.IsDeleted)
            .Select(s => s.SharedWithUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Build the full list of involved users: owner + payer + completer + shared
        var allUserIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(task.CreatedBy)) allUserIds.Add(task.CreatedBy);
        if (!string.IsNullOrEmpty(resolvedPaidByUserId)) allUserIds.Add(resolvedPaidByUserId);
        if (!string.IsNullOrEmpty(currentUserId)) allUserIds.Add(currentUserId);
        foreach (var uid in sharedUserIds) allUserIds.Add(uid);

        if (allUserIds.Count > 0 && bill.Amount > 0)
        {
            var percentage = Math.Round(100m / allUserIds.Count, 2);
            foreach (var userId in allUserIds)
            {
                var splitAmount = Math.Round(bill.Amount * percentage / 100m, 2);
                var isPayer = string.Equals(userId, resolvedPaidByUserId, StringComparison.OrdinalIgnoreCase);

                bill.Splits.Add(new BillSplit
                {
                    BillId = bill.Id,
                    UserId = userId,
                    Percentage = percentage,
                    Amount = splitAmount,
                    Status = isPayer ? SplitStatus.Paid : SplitStatus.Unpaid
                });
            }
        }

        dbContext.Bills.Add(bill);

        // Auto-share the bill with all involved users who are not the current user
        var usersToShare = allUserIds.Where(uid => !string.Equals(uid, currentUserId, StringComparison.OrdinalIgnoreCase));
        foreach (var userId in usersToShare)
        {
            dbContext.EntityShares.Add(new EntityShare
            {
                EntityType = EntityTypes.Bill,
                EntityId = bill.Id,
                SharedWithUserId = userId,
                Permission = SharePermission.View
            });
        }

        return bill;
    }
}
