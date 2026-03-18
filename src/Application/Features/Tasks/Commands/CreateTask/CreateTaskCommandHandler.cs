using MediatR;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IOccurrenceScheduler occurrenceScheduler,
    IPublisher publisher)
    : IRequestHandler<CreateTaskCommand, Guid>
{
    public async Task<Guid> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;

        var task = new HouseholdTask
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Category = request.Category,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            IsRecurring = request.IsRecurring,
            DueDate = request.DueDate,
            AssignedToUserId = request.AssignedToUserId ?? currentUserId,
            AutoCreateBill = request.AutoCreateBill,
            DefaultBillAmount = request.DefaultBillAmount,
            DefaultBillCurrency = request.DefaultBillCurrency,
            DefaultBillCategory = request.DefaultBillCategory,
            DefaultBillTitle = request.DefaultBillTitle,
            DefaultBillPaidByUserId = request.DefaultBillPaidByUserId,
            DefaultBudgetId = request.DefaultBudgetId
        };

        if (request.IsRecurring && request.RecurrenceType.HasValue && request.RecurrenceStartDate.HasValue)
        {
            var pattern = new RecurrencePattern
            {
                HouseholdTaskId = task.Id,
                Type = request.RecurrenceType.Value,
                Interval = request.Interval ?? 1,
                StartDate = request.RecurrenceStartDate.Value,
                EndDate = request.RecurrenceEndDate
            };

            // When no rotation assignees provided, default to current user
            var assigneeIds = request.AssigneeUserIds is { Count: > 0 }
                ? request.AssigneeUserIds
                : currentUserId is not null ? [currentUserId] : [];

            for (var i = 0; i < assigneeIds.Count; i++)
            {
                pattern.Assignees.Add(new RecurrenceAssignee
                {
                    RecurrencePatternId = pattern.Id,
                    UserId = assigneeIds[i],
                    Order = i
                });
            }

            task.RecurrencePattern = pattern;
        }

        dbContext.HouseholdTasks.Add(task);

        // Auto-share: when task is assigned to another user, give them Edit access
        // so they can view, edit, and track the task
        if (!string.IsNullOrEmpty(task.AssignedToUserId)
            && !string.IsNullOrEmpty(currentUserId)
            && task.AssignedToUserId != currentUserId)
        {
            dbContext.EntityShares.Add(new EntityShare
            {
                EntityType = EntityTypes.HouseholdTask,
                EntityId = task.Id,
                SharedWithUserId = task.AssignedToUserId,
                Permission = SharePermission.Edit
            });
        }

        // Also auto-share with all recurrence assignees who are not the creator
        if (request.IsRecurring && request.AssigneeUserIds is { Count: > 0 })
        {
            foreach (var assigneeId in request.AssigneeUserIds.Where(id =>
                !string.IsNullOrEmpty(id)
                && id != currentUserId
                && id != task.AssignedToUserId))
            {
                dbContext.EntityShares.Add(new EntityShare
                {
                    EntityType = EntityTypes.HouseholdTask,
                    EntityId = task.Id,
                    SharedWithUserId = assigneeId,
                    Permission = SharePermission.Edit
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Generate initial occurrences for recurring tasks immediately
        if (task.IsRecurring && task.RecurrencePattern is not null)
        {
            await occurrenceScheduler.SyncOccurrencesAsync(task.Id, cancellationToken);
        }
        else
        {
            var occurrence = new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = task.DueDate ?? DateOnly.FromDateTime(DateTime.Now),
                Status = OccurrenceStatus.Pending,
                AssignedToUserId = task.AssignedToUserId
            };

            dbContext.TaskOccurrences.Add(occurrence);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await publisher.Publish(new TaskCreatedEvent(task.Id, task.Title), cancellationToken);

        return task.Id;
    }
}
