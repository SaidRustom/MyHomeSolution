using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Tasks.Common;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Tasks.Queries.GetTaskById;

public sealed class GetTaskByIdQueryHandler(
    IApplicationDbContext dbContext,
    IIdentityService identityService)
    : IRequestHandler<GetTaskByIdQuery, TaskDetailDto>
{
    public async Task<TaskDetailDto> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await dbContext.HouseholdTasks
            .AsNoTracking()
            .Include(t => t.RecurrencePattern!)
                .ThenInclude(rp => rp.Assignees)
            .Include(t => t.Occurrences.Where(o => !o.IsDeleted).OrderBy(o => o.DueDate))
                .ThenInclude(o => o.Bill!)
                    .ThenInclude(b => b.Splits)
            .Where(t => !t.IsDeleted)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(HouseholdTask), request.Id);

        // Collect all user IDs we need to resolve
        var userIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(task.AssignedToUserId)) userIds.Add(task.AssignedToUserId);
        if (!string.IsNullOrEmpty(task.CreatedBy)) userIds.Add(task.CreatedBy);
        if (!string.IsNullOrEmpty(task.DefaultBillPaidByUserId)) userIds.Add(task.DefaultBillPaidByUserId);

        foreach (var occ in task.Occurrences)
        {
            if (!string.IsNullOrEmpty(occ.AssignedToUserId)) userIds.Add(occ.AssignedToUserId);
            if (!string.IsNullOrEmpty(occ.CompletedByUserId)) userIds.Add(occ.CompletedByUserId);
        }

        if (task.RecurrencePattern?.Assignees is { Count: > 0 })
        {
            foreach (var a in task.RecurrencePattern.Assignees)
                userIds.Add(a.UserId);
        }

        // Resolve user details in bulk
        var userDetails = new Dictionary<string, (string FullName, string? AvatarUrl)>(StringComparer.OrdinalIgnoreCase);
        if (userIds.Count > 0)
        {
            foreach (var uid in userIds)
            {
                var detail = await identityService.GetUserByIdAsync(uid, cancellationToken);
                if (detail is not null)
                    userDetails[uid] = (detail.FullName, detail.AvatarUrl);
            }
        }

        string? ResolveName(string? id) => id is not null && userDetails.TryGetValue(id, out var d) ? d.FullName : null;
        string? ResolveAvatar(string? id) => id is not null && userDetails.TryGetValue(id, out var d) ? d.AvatarUrl : null;

        return new TaskDetailDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Priority = task.Priority,
            Category = task.Category,
            EstimatedDurationMinutes = task.EstimatedDurationMinutes,
            IsRecurring = task.IsRecurring,
            IsActive = task.IsActive,
            DueDate = task.DueDate,
            AssignedToUserId = task.AssignedToUserId,
            AssignedToUserFullName = ResolveName(task.AssignedToUserId),
            AssignedToUserAvatarUrl = ResolveAvatar(task.AssignedToUserId),
            CreatedByUserId = task.CreatedBy,
            CreatedByUserFullName = ResolveName(task.CreatedBy),
            CreatedAt = task.CreatedAt,
            AutoCreateBill = task.AutoCreateBill,
            DefaultBillAmount = task.DefaultBillAmount,
            DefaultBillCurrency = task.DefaultBillCurrency,
            DefaultBillCategory = task.DefaultBillCategory,
            DefaultBillTitle = task.DefaultBillTitle,
            DefaultBillPaidByUserId = task.DefaultBillPaidByUserId,
            DefaultBillPaidByUserFullName = ResolveName(task.DefaultBillPaidByUserId),
            RecurrencePattern = task.RecurrencePattern is not null
                ? new RecurrencePatternDto
                {
                    Id = task.RecurrencePattern.Id,
                    Type = task.RecurrencePattern.Type,
                    Interval = task.RecurrencePattern.Interval,
                    StartDate = task.RecurrencePattern.StartDate,
                    EndDate = task.RecurrencePattern.EndDate,
                    AssigneeUserIds = task.RecurrencePattern.Assignees
                        .OrderBy(a => a.Order)
                        .Select(a => a.UserId)
                        .ToList(),
                    Assignees = task.RecurrencePattern.Assignees
                        .OrderBy(a => a.Order)
                        .Select(a => new RecurrenceAssigneeDto
                        {
                            UserId = a.UserId,
                            FullName = ResolveName(a.UserId),
                            AvatarUrl = ResolveAvatar(a.UserId),
                            Order = a.Order
                        })
                        .ToList()
                }
                : null,
            Occurrences = task.Occurrences.Select(o => new OccurrenceDto
            {
                Id = o.Id,
                DueDate = o.DueDate,
                Status = o.Status,
                AssignedToUserId = o.AssignedToUserId,
                AssignedToUserFullName = ResolveName(o.AssignedToUserId),
                AssignedToUserAvatarUrl = ResolveAvatar(o.AssignedToUserId),
                CompletedAt = o.CompletedAt,
                CompletedByUserId = o.CompletedByUserId,
                CompletedByUserFullName = ResolveName(o.CompletedByUserId),
                CompletedByUserAvatarUrl = ResolveAvatar(o.CompletedByUserId),
                Notes = o.Notes,
                BillId = o.BillId,
                Bill = o.Bill is not null
                    ? new OccurrenceBillBriefDto
                    {
                        Id = o.Bill.Id,
                        Title = o.Bill.Title,
                        Amount = o.Bill.Amount,
                        Currency = o.Bill.Currency,
                        Category = o.Bill.Category,
                        BillDate = o.Bill.BillDate,
                        TotalSplits = o.Bill.Splits.Count,
                        PaidSplits = o.Bill.Splits.Count(s => s.Status == SplitStatus.Paid)
                    }
                    : null
            }).ToList()
        };
    }
}
