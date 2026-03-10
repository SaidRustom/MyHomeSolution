using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Tasks.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Occurrences.Queries.GetUpcomingOccurrences;

public sealed class GetUpcomingOccurrencesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetUpcomingOccurrencesQuery, PaginatedList<CalendarOccurrenceDto>>
{
    public async Task<PaginatedList<CalendarOccurrenceDto>> Handle(
        GetUpcomingOccurrencesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var today = dateTimeProvider.Today;

        var sharedTaskIds = dbContext.EntityShares
            .Where(s => s.EntityType == EntityTypes.HouseholdTask
                && s.SharedWithUserId == userId
                && !s.IsDeleted)
            .Select(s => s.EntityId);

        // When a date range is provided, scope to that range (plus overdue);
        // otherwise fall back to "today and forward" for general upcoming.
        var rangeStart = request.StartDate ?? today;
        var rangeEnd = request.EndDate;

        var query = dbContext.TaskOccurrences
            .AsNoTracking()
            .Where(o => !o.IsDeleted
                && !o.HouseholdTask.IsDeleted
                && o.HouseholdTask.IsActive)
            .Where(o => o.HouseholdTask.CreatedBy == userId || sharedTaskIds.Contains(o.HouseholdTaskId))
            .Where(o =>
                // Within requested range
                (o.DueDate >= rangeStart && (rangeEnd == null || o.DueDate <= rangeEnd)
                    && o.Status == OccurrenceStatus.Pending)
                // Overdue: past-due occurrences not completed/skipped
                || (o.DueDate < today
                    && o.Status != OccurrenceStatus.Completed
                    && o.Status != OccurrenceStatus.Skipped))
            .OrderBy(o => o.DueDate)
            .ThenByDescending(o => o.HouseholdTask.Priority)
            .Select(o => new CalendarOccurrenceDto
            {
                Id = o.Id,
                TaskId = o.HouseholdTaskId,
                TaskTitle = o.HouseholdTask.Title,
                TaskPriority = o.HouseholdTask.Priority,
                TaskCategory = o.HouseholdTask.Category,
                EstimatedDurationMinutes = o.HouseholdTask.EstimatedDurationMinutes,
                DueDate = o.DueDate,
                Status = o.Status,
                AssignedToUserId = o.AssignedToUserId,
                BillId = o.BillId
            });

        return await PaginatedList<CalendarOccurrenceDto>.CreateAsync(
            query, request.PageNumber, request.PageSize, cancellationToken);
    }
}
