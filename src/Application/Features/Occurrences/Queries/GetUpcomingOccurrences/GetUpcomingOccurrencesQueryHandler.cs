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

        var query = dbContext.TaskOccurrences
            .AsNoTracking()
            .Where(o => !o.IsDeleted
                && o.DueDate >= today
                && !o.HouseholdTask.IsDeleted
                && o.HouseholdTask.IsActive
                && o.Status == OccurrenceStatus.Pending)
            .Where(o => o.HouseholdTask.CreatedBy == userId || sharedTaskIds.Contains(o.HouseholdTaskId))
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
