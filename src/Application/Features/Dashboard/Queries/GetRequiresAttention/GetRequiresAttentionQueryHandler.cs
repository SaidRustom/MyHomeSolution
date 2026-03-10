using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Dashboard.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Dashboard.Queries.GetRequiresAttention;

public sealed class GetRequiresAttentionQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetRequiresAttentionQuery, RequiresAttentionDto>
{
    public async Task<RequiresAttentionDto> Handle(
        GetRequiresAttentionQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var unpaidBills = await GetUnpaidBillsAsync(userId, cancellationToken);
        var urgentTasks = await GetUrgentTasksAsync(userId, cancellationToken);

        return new RequiresAttentionDto
        {
            UnpaidBills = unpaidBills,
            UrgentTasks = urgentTasks
        };
    }

    private async Task<IReadOnlyList<AttentionBillDto>> GetUnpaidBillsAsync(
        string userId, CancellationToken cancellationToken)
    {
        var today = dateTimeProvider.UtcNow;

        // Bills where the user is the payer or has a split, and no one has fully paid
        var splitBillIds = dbContext.BillSplits
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == SplitStatus.Unpaid)
            .Select(s => s.BillId);

        var sharedBillIds = dbContext.EntityShares
            .AsNoTracking()
            .Where(s => s.EntityType == EntityTypes.Bill
                && s.SharedWithUserId == userId
                && !s.IsDeleted)
            .Select(s => s.EntityId);

        // Bill IDs linked to overdue task occurrences — these are already surfaced
        // in the urgent tasks section, so exclude them from unpaid bills
        var overdueBillIds = dbContext.TaskOccurrences
            .AsNoTracking()
            .Where(o => !o.IsDeleted && o.BillId != null && o.Status == OccurrenceStatus.Overdue)
            .Select(o => o.BillId!.Value);

        // Bill IDs linked to completed task occurrences — these should be shown
        var completedTaskBillIds = dbContext.TaskOccurrences
            .AsNoTracking()
            .Where(o => !o.IsDeleted && o.BillId != null && o.Status == OccurrenceStatus.Completed)
            .Select(o => o.BillId!.Value);

        var bills = await dbContext.Bills
            .AsNoTracking()
            .Include(b => b.Splits)
            .Where(b => !b.IsDeleted)
            .Where(b => b.CreatedBy == userId
                || b.PaidByUserId == userId
                || splitBillIds.Contains(b.Id)
                || sharedBillIds.Contains(b.Id))
            .Where(b => b.Splits.Any(s => s.Status == SplitStatus.Unpaid))
            // Exclude bills linked to overdue occurrences (shown as urgent tasks)
            .Where(b => !overdueBillIds.Contains(b.Id))
            // Only include task-linked bills if the linked occurrence is completed,
            // but still include non-task-linked bills (e.g. standalone bills)
            .Where(b => b.RelatedEntityType != EntityTypes.TaskOccurrence
                || completedTaskBillIds.Contains(b.Id))
            .OrderByDescending(b => b.BillDate)
            .Take(10)
            .ToListAsync(cancellationToken);

        var payerIds = bills.Select(b => b.PaidByUserId).Distinct();
        var nameMap = await identityService.GetUserFullNamesByIdsAsync(payerIds, cancellationToken);

        return bills.Select(b => new AttentionBillDto
        {
            Id = b.Id,
            Title = b.Title,
            Amount = b.Amount,
            Currency = b.Currency,
            Category = b.Category,
            BillDate = b.BillDate,
            PaidByUserFullName = nameMap.GetValueOrDefault(b.PaidByUserId),
            CreatedAt = b.CreatedAt
        }).ToList();
    }

    private async Task<IReadOnlyList<AttentionTaskDto>> GetUrgentTasksAsync(
        string userId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow.DateTime);
        var dayAfterTomorrow = today.AddDays(2);

        // Tasks owned by or shared with the user
        var sharedTaskIds = dbContext.EntityShares
            .AsNoTracking()
            .Where(s => s.EntityType == EntityTypes.HouseholdTask
                && s.SharedWithUserId == userId
                && !s.IsDeleted)
            .Select(s => s.EntityId);

        var occurrences = await dbContext.TaskOccurrences
            .AsNoTracking()
            .Include(o => o.HouseholdTask)
            .Where(o => !o.IsDeleted && !o.HouseholdTask.IsDeleted)
            .Where(o => o.HouseholdTask.CreatedBy == userId
                || o.AssignedToUserId == userId
                || sharedTaskIds.Contains(o.HouseholdTaskId))
            .Where(o => o.HouseholdTask.Priority >= TaskPriority.High)
            .Where(o => o.DueDate >= today && o.DueDate <= dayAfterTomorrow)
            .Where(o => o.Status != OccurrenceStatus.Completed && o.Status != OccurrenceStatus.Skipped)
            .OrderBy(o => o.DueDate)
            .ThenByDescending(o => o.HouseholdTask.Priority)
            .Take(10)
            .ToListAsync(cancellationToken);

        var assigneeIds = occurrences
            .Where(o => !string.IsNullOrEmpty(o.AssignedToUserId))
            .Select(o => o.AssignedToUserId!)
            .Distinct();
        var nameMap = await identityService.GetUserFullNamesByIdsAsync(assigneeIds, cancellationToken);

        return occurrences.Select(o => new AttentionTaskDto
        {
            TaskId = o.HouseholdTaskId,
            OccurrenceId = o.Id,
            Title = o.HouseholdTask.Title,
            Priority = o.HouseholdTask.Priority,
            Category = o.HouseholdTask.Category,
            DueDate = o.DueDate,
            Status = o.Status,
            AssignedToUserFullName = o.AssignedToUserId is not null
                ? nameMap.GetValueOrDefault(o.AssignedToUserId)
                : null
        }).ToList();
    }
}
