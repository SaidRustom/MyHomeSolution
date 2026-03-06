using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Tasks.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Tasks.Queries.GetTasks;

public sealed class GetTasksQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetTasksQuery, PaginatedList<TaskBriefDto>>
{
    public async Task<PaginatedList<TaskBriefDto>> Handle(
        GetTasksQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var query = dbContext.HouseholdTasks
            .AsNoTracking()
            .Include(t => t.Occurrences)
            .Where(t => !t.IsDeleted && t.IsActive);

        if (userId is not null)
        {
            var sharedTaskIds = dbContext.EntityShares
                .Where(s => s.EntityType == EntityTypes.HouseholdTask
                    && s.SharedWithUserId == userId
                    && !s.IsDeleted)
                .Select(s => s.EntityId);

            query = query.Where(t => t.CreatedBy == userId || sharedTaskIds.Contains(t.Id));
        }

        if (request.Category.HasValue)
            query = query.Where(t => t.Category == request.Category.Value);

        if (request.Priority.HasValue)
            query = query.Where(t => t.Priority == request.Priority.Value);

        if (request.IsRecurring.HasValue)
            query = query.Where(t => t.IsRecurring == request.IsRecurring.Value);

        if (!string.IsNullOrWhiteSpace(request.AssignedToUserId))
            query = query.Where(t => t.AssignedToUserId == request.AssignedToUserId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(t => t.Title.Contains(request.SearchTerm)
                                     || (t.Description != null && t.Description.Contains(request.SearchTerm)));

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value;
            query = query.Where(t =>
                (t.IsRecurring && t.Occurrences.Any(o => o.DueDate >= from)) ||
                (!t.IsRecurring && t.DueDate >= from));
        }

        if (request.ToDate.HasValue)
        {
            var to = request.ToDate.Value;
            query = query.Where(t =>
                (t.IsRecurring && t.Occurrences.Any(o => o.DueDate <= to)) ||
                (!t.IsRecurring && t.DueDate <= to));
        }

        if (request.NotCompletedOnly == true)
        {
            query = query.Where(t =>
                (t.IsRecurring && t.Occurrences.Any(o => o.Status != OccurrenceStatus.Completed)) ||
                (!t.IsRecurring && t.DueDate != null));
        }

        var projected = query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .Select(t => new TaskBriefDto
            {
                Id = t.Id,
                Title = t.Title,
                Priority = t.Priority,
                Category = t.Category,
                IsRecurring = t.IsRecurring,
                IsActive = t.IsActive,
                EstimatedDurationMinutes = t.EstimatedDurationMinutes,
                AssignedToUserId = t.AssignedToUserId,
                NextDueDate = t.IsRecurring
                    ? t.Occurrences
                        .Where(o => o.Status == Domain.Enums.OccurrenceStatus.Pending)
                        .OrderBy(o => o.DueDate)
                        .Select(o => (DateOnly?)o.DueDate)
                        .FirstOrDefault()
                    : t.DueDate
            });

        return await PaginatedList<TaskBriefDto>.CreateAsync(
            projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
