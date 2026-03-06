using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Tasks.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Tasks.Queries.GetTodayTasks;

public sealed class GetTodayTasksQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetTodayTasksQuery, IReadOnlyCollection<TodayTaskDto>>
{
    public async Task<IReadOnlyCollection<TodayTaskDto>> Handle(
        GetTodayTasksQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var today = dateTimeProvider.Today;

        var sharedTaskIds = dbContext.EntityShares
            .Where(s => s.EntityType == EntityTypes.HouseholdTask
                && s.SharedWithUserId == userId
                && !s.IsDeleted)
            .Select(s => s.EntityId);

        // Recurring tasks: get tasks that have qualifying occurrences (today + overdue past)
        var recurringTasks = await dbContext.HouseholdTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.IsActive && t.IsRecurring)
            .Where(t => t.CreatedBy == userId || sharedTaskIds.Contains(t.Id))
            .Where(t => t.Occurrences.Any(o =>
                !o.IsDeleted &&
                (o.DueDate == today ||
                 (o.DueDate < today &&
                  o.Status != OccurrenceStatus.Completed &&
                  o.Status != OccurrenceStatus.Skipped))))
            .Select(t => new TodayTaskDto
            {
                TaskId = t.Id,
                Title = t.Title,
                Description = t.Description,
                Priority = t.Priority,
                Category = t.Category,
                EstimatedDurationMinutes = t.EstimatedDurationMinutes,
                IsRecurring = true,
                AssignedToUserId = t.AssignedToUserId,
                Occurrences = t.Occurrences
                    .Where(o => !o.IsDeleted &&
                        (o.DueDate == today ||
                         (o.DueDate < today &&
                          o.Status != OccurrenceStatus.Completed &&
                          o.Status != OccurrenceStatus.Skipped)))
                    .OrderBy(o => o.DueDate)
                    .Select(o => new OccurrenceDto
                    {
                        Id = o.Id,
                        DueDate = o.DueDate,
                        Status = o.Status,
                        AssignedToUserId = o.AssignedToUserId,
                        CompletedAt = o.CompletedAt,
                        CompletedByUserId = o.CompletedByUserId,
                        Notes = o.Notes,
                        BillId = o.BillId
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        // Non-recurring tasks: due today or overdue (past, not completed/skipped)
        var nonRecurringRaw = await dbContext.HouseholdTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.IsActive && !t.IsRecurring)
            .Where(t => t.CreatedBy == userId || sharedTaskIds.Contains(t.Id))
            .Where(t => t.DueDate.HasValue &&
                (t.DueDate.Value == today ||
                 t.DueDate.Value < today))
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                t.Priority,
                t.Category,
                t.EstimatedDurationMinutes,
                t.AssignedToUserId
            })
            .ToListAsync(cancellationToken);

        var nonRecurringTasks = nonRecurringRaw.Select(t => new TodayTaskDto
        {
            TaskId = t.Id,
            Title = t.Title,
            Description = t.Description,
            Priority = t.Priority,
            Category = t.Category,
            EstimatedDurationMinutes = t.EstimatedDurationMinutes,
            IsRecurring = false,
            AssignedToUserId = t.AssignedToUserId,
            Occurrences = []
        }).ToList();

        return [.. recurringTasks, .. nonRecurringTasks.OrderByDescending(t => t.Priority)];
    }
}
