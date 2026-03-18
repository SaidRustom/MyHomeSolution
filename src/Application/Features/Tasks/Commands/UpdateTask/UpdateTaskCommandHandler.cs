using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IOccurrenceScheduler occurrenceScheduler,
    IPublisher publisher)
    : IRequestHandler<UpdateTaskCommand>
{
    public async Task Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;

        var task = await dbContext.HouseholdTasks
            .Include(t => t.RecurrencePattern!)
                .ThenInclude(rp => rp.Assignees)
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(HouseholdTask), request.Id);

        var recurrenceChanged = DetectRecurrenceChange(task, request);

        task.Title = request.Title;
        task.Description = request.Description;
        task.Priority = request.Priority;
        task.Category = request.Category;
        task.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
        task.IsActive = request.IsActive;
        task.DueDate = request.DueDate;
        task.AssignedToUserId = request.AssignedToUserId;
        task.IsRecurring = request.IsRecurring;
        task.AutoCreateBill = request.AutoCreateBill;
        task.DefaultBillAmount = request.DefaultBillAmount;
        task.DefaultBillCurrency = request.DefaultBillCurrency;
        task.DefaultBillCategory = request.DefaultBillCategory;
        task.DefaultBillTitle = request.DefaultBillTitle;
        task.DefaultBillPaidByUserId = request.DefaultBillPaidByUserId;
        task.DefaultBudgetId = request.DefaultBudgetId;

        if (request.IsRecurring && request.RecurrenceType.HasValue && request.RecurrenceStartDate.HasValue)
        {
            if (task.RecurrencePattern is null)
            {
                var pattern = new RecurrencePattern
                {
                    HouseholdTaskId = task.Id,
                    Type = request.RecurrenceType.Value,
                    Interval = request.Interval ?? 1,
                    StartDate = request.RecurrenceStartDate.Value,
                    EndDate = request.RecurrenceEndDate
                };
                dbContext.RecurrencePatterns.Add(pattern);
                task.RecurrencePattern = pattern;
            }
            else
            {
                task.RecurrencePattern.Type = request.RecurrenceType.Value;
                task.RecurrencePattern.Interval = request.Interval ?? 1;
                task.RecurrencePattern.StartDate = request.RecurrenceStartDate.Value;
                task.RecurrencePattern.EndDate = request.RecurrenceEndDate;
            }

            SyncAssignees(dbContext, task.RecurrencePattern, request.AssigneeUserIds ?? []);
        }
        else if (!request.IsRecurring && task.RecurrencePattern is not null)
        {
            dbContext.RecurrenceAssignees.RemoveRange(task.RecurrencePattern.Assignees);
            dbContext.RecurrencePatterns.Remove(task.RecurrencePattern);
            task.RecurrencePattern = null;
            recurrenceChanged = true;
        }

        // Auto-share: when task is assigned to another user, ensure they have Edit access
        await EnsureAssigneeShareAsync(task.Id, task.AssignedToUserId, currentUserId, cancellationToken);

        // Also ensure all recurrence assignees have shares
        if (request.IsRecurring && request.AssigneeUserIds is { Count: > 0 })
        {
            foreach (var assigneeId in request.AssigneeUserIds.Where(id =>
                !string.IsNullOrEmpty(id) && id != currentUserId))
            {
                await EnsureAssigneeShareAsync(task.Id, assigneeId, currentUserId, cancellationToken);
            }
        }

        // Persist task + pattern changes BEFORE syncing occurrences.
        // The scheduler opens its own DB scope and reads the latest persisted state.
        await dbContext.SaveChangesAsync(cancellationToken);

        if (recurrenceChanged)
        {
            await occurrenceScheduler.SyncOccurrencesAsync(task.Id, cancellationToken);
        }

        await publisher.Publish(new TaskUpdatedEvent(task.Id, task.Title), cancellationToken);
    }

    private static bool DetectRecurrenceChange(HouseholdTask task, UpdateTaskCommand request)
    {
        if (task.IsRecurring != request.IsRecurring)
            return true;

        if (!request.IsRecurring)
            return false;

        var pattern = task.RecurrencePattern;
        if (pattern is null && request.RecurrenceType.HasValue)
            return true;

        if (pattern is null)
            return false;

        if (pattern.Type != request.RecurrenceType)
            return true;
        if (pattern.Interval != (request.Interval ?? 1))
            return true;
        if (pattern.StartDate != request.RecurrenceStartDate)
            return true;
        if (pattern.EndDate != request.RecurrenceEndDate)
            return true;

        var existingIds = pattern.Assignees.OrderBy(a => a.Order).Select(a => a.UserId).ToList();
        var newIds = request.AssigneeUserIds ?? [];
        if (!existingIds.SequenceEqual(newIds))
            return true;

        return false;
    }

    private static void SyncAssignees(IApplicationDbContext dbContext, RecurrencePattern pattern, List<string> newUserIds)
    {
        var existingIds = pattern.Assignees.OrderBy(a => a.Order).Select(a => a.UserId).ToList();
        if (existingIds.SequenceEqual(newUserIds))
            return;

        foreach (var assignee in pattern.Assignees.ToList())
        {
            dbContext.RecurrenceAssignees.Remove(assignee);
        }

        for (var i = 0; i < newUserIds.Count; i++)
        {
            pattern.Assignees.Add(new RecurrenceAssignee
            {
                RecurrencePatternId = pattern.Id,
                UserId = newUserIds[i],
                Order = i
            });
        }

        pattern.LastAssigneeIndex = -1;
    }

    private async Task EnsureAssigneeShareAsync(
        Guid taskId, string? assigneeId, string? currentUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(assigneeId) || string.IsNullOrEmpty(currentUserId)
            || assigneeId == currentUserId)
            return;

        var alreadyShared = await dbContext.EntityShares
            .AnyAsync(s => s.EntityType == EntityTypes.HouseholdTask
                && s.EntityId == taskId
                && s.SharedWithUserId == assigneeId
                && !s.IsDeleted, cancellationToken);

        if (!alreadyShared)
        {
            dbContext.EntityShares.Add(new EntityShare
            {
                EntityType = EntityTypes.HouseholdTask,
                EntityId = taskId,
                SharedWithUserId = assigneeId,
                Permission = SharePermission.Edit
            });
        }
    }
}
