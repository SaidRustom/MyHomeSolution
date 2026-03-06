using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Tasks.Common;

namespace MyHomeSolution.Application.Features.Occurrences.Queries.GetOccurrencesByDateRange;

public sealed class GetOccurrencesByDateRangeQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetOccurrencesByDateRangeQuery, IReadOnlyCollection<CalendarOccurrenceDto>>
{
    public async Task<IReadOnlyCollection<CalendarOccurrenceDto>> Handle(
        GetOccurrencesByDateRangeQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var sharedTaskIds = dbContext.EntityShares
            .Where(s => s.EntityType == EntityTypes.HouseholdTask
                && s.SharedWithUserId == userId
                && !s.IsDeleted)
            .Select(s => s.EntityId);

        var query = dbContext.TaskOccurrences
            .AsNoTracking()
            .Where(o => !o.IsDeleted
                && o.DueDate >= request.StartDate
                && o.DueDate <= request.EndDate
                && !o.HouseholdTask.IsDeleted
                && o.HouseholdTask.IsActive)
            .Where(o => o.HouseholdTask.CreatedBy == userId || sharedTaskIds.Contains(o.HouseholdTaskId));

        if (!string.IsNullOrWhiteSpace(request.AssignedToUserId))
            query = query.Where(o => o.AssignedToUserId == request.AssignedToUserId);

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        return await query
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
            })
            .ToListAsync(cancellationToken);
    }
}
