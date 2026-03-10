using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Tasks.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Occurrences.Queries.GetOccurrencesByDateRange;

public sealed class GetOccurrencesByDateRangeQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IIdentityService identityService)
    : IRequestHandler<GetOccurrencesByDateRangeQuery, IReadOnlyCollection<CalendarOccurrenceDto>>
{
    public async Task<IReadOnlyCollection<CalendarOccurrenceDto>> Handle(
        GetOccurrencesByDateRangeQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var today = dateTimeProvider.Today;

        var sharedTaskIds = dbContext.EntityShares
            .Where(s => s.EntityType == EntityTypes.HouseholdTask
                && s.SharedWithUserId == userId
                && !s.IsDeleted)
            .Select(s => s.EntityId);

        var includesCurrentDay = request.StartDate <= today && request.EndDate >= today;

        // Base query: occurrences within the requested date range
        // PLUS overdue occurrences (past-due, not completed/skipped) when the range includes today
        var query = dbContext.TaskOccurrences
            .AsNoTracking()
            .Where(o => !o.IsDeleted
                && !o.HouseholdTask.IsDeleted
                && o.HouseholdTask.IsActive)
            .Where(o => o.HouseholdTask.CreatedBy == userId || sharedTaskIds.Contains(o.HouseholdTaskId))
            .Where(o =>
                // Normal range: occurrence falls within requested dates
                (o.DueDate >= request.StartDate && o.DueDate <= request.EndDate)
                // Overdue: past-due occurrences not completed/skipped shown under today
                || (includesCurrentDay
                    && o.DueDate < today
                    && o.Status != OccurrenceStatus.Completed
                    && o.Status != OccurrenceStatus.Skipped));

        if (!string.IsNullOrWhiteSpace(request.AssignedToUserId))
            query = query.Where(o => o.AssignedToUserId == request.AssignedToUserId);

        // View mode filters
        if (request.MyTasks == true && userId is not null)
            query = query.Where(o => o.AssignedToUserId == userId);

        // "Assigned by me": tasks created by the current user but assigned to someone else
        if (request.AssignedByMe == true && userId is not null)
            query = query.Where(o => o.HouseholdTask.CreatedBy == userId
                && o.AssignedToUserId != null
                && o.AssignedToUserId != userId);

        // "Private": tasks owned by user with no shares
        if (request.Private == true && userId is not null)
            query = query.Where(o => o.HouseholdTask.CreatedBy == userId
                && !dbContext.EntityShares.Any(s =>
                    s.EntityType == EntityTypes.HouseholdTask
                    && s.EntityId == o.HouseholdTaskId
                    && !s.IsDeleted));

        // "Shared": tasks that have at least one share
        if (request.Shared == true && userId is not null)
            query = query.Where(o => dbContext.EntityShares.Any(s =>
                s.EntityType == EntityTypes.HouseholdTask
                && s.EntityId == o.HouseholdTaskId
                && !s.IsDeleted));

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        // Additional filters
        if (request.IsRecurring.HasValue)
            query = query.Where(o => o.HouseholdTask.IsRecurring == request.IsRecurring.Value);

        if (request.HasBill == true)
            query = query.Where(o => o.BillId != null);
        else if (request.HasBill == false)
            query = query.Where(o => o.BillId == null);

        if (request.Category.HasValue)
            query = query.Where(o => o.HouseholdTask.Category == request.Category.Value);

        if (request.Priority.HasValue)
            query = query.Where(o => o.HouseholdTask.Priority == request.Priority.Value);

        var rawResults = await query
            .OrderBy(o => o.DueDate)
            .ThenByDescending(o => o.HouseholdTask.Priority)
            .Select(o => new
            {
                o.Id,
                TaskId = o.HouseholdTaskId,
                o.HouseholdTask.Title,
                o.HouseholdTask.Priority,
                Category = o.HouseholdTask.Category,
                o.HouseholdTask.EstimatedDurationMinutes,
                o.DueDate,
                o.Status,
                o.AssignedToUserId,
                o.CompletedByUserId,
                o.BillId,
                IsRecurring = o.HouseholdTask.IsRecurring,
                HasBill = o.BillId != null,
                IsPrivate = !dbContext.EntityShares.Any(s =>
                    s.EntityType == EntityTypes.HouseholdTask
                    && s.EntityId == o.HouseholdTaskId
                    && !s.IsDeleted)
            })
            .ToListAsync(cancellationToken);

        // Resolve user names in bulk
        var allUserIds = rawResults
            .SelectMany(r => new[] { r.AssignedToUserId, r.CompletedByUserId })
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var nameMap = allUserIds.Count > 0
            ? await identityService.GetUserFullNamesByIdsAsync(allUserIds!, cancellationToken)
            : new Dictionary<string, string>();

        return rawResults.Select(o => new CalendarOccurrenceDto
        {
            Id = o.Id,
            TaskId = o.TaskId,
            TaskTitle = o.Title,
            TaskPriority = o.Priority,
            TaskCategory = o.Category,
            EstimatedDurationMinutes = o.EstimatedDurationMinutes,
            DueDate = o.DueDate,
            Status = o.Status,
            AssignedToUserId = o.AssignedToUserId,
            AssignedToUserFullName = o.AssignedToUserId is not null && nameMap.TryGetValue(o.AssignedToUserId, out var an) ? an : null,
            CompletedByUserId = o.CompletedByUserId,
            CompletedByUserFullName = o.CompletedByUserId is not null && nameMap.TryGetValue(o.CompletedByUserId, out var cn) ? cn : null,
            BillId = o.BillId,
            IsRecurring = o.IsRecurring,
            HasBill = o.HasBill,
            IsPrivate = o.IsPrivate
        }).ToList();
    }
}
