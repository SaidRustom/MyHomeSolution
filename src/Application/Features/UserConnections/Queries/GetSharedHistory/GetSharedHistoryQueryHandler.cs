using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Common;
using MyHomeSolution.Application.Features.Budgets.Common;
using MyHomeSolution.Application.Features.ShoppingLists.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.UserConnections.Queries.GetSharedHistory;

public sealed class GetSharedHistoryQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService)
    : IRequestHandler<GetSharedHistoryQuery, SharedHistoryDto>
{
    public async Task<SharedHistoryDto> Handle(
        GetSharedHistoryQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var targetUserId = request.UserId;

        // Get target user info
        var targetUser = await identityService.GetUserByIdAsync(targetUserId, cancellationToken)
            ?? throw new NotFoundException("User", targetUserId);

        // Find connection between the two users
        var connection = await dbContext.UserConnections
            .AsNoTracking()
            .Where(c => c.Status == ConnectionStatus.Accepted)
            .Where(c =>
                (c.RequesterId == currentUserId && c.AddresseeId == targetUserId) ||
                (c.RequesterId == targetUserId && c.AddresseeId == currentUserId))
            .FirstOrDefaultAsync(cancellationToken);

        // Gather all entity shares between the two users (both directions)
        var sharedByMeIds = await dbContext.EntityShares
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .Where(s => s.CreatedBy == currentUserId && s.SharedWithUserId == targetUserId)
            .Select(s => new { s.EntityId, s.EntityType })
            .ToListAsync(cancellationToken);

        var sharedWithMeIds = await dbContext.EntityShares
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .Where(s => s.CreatedBy == targetUserId && s.SharedWithUserId == currentUserId)
            .Select(s => new { s.EntityId, s.EntityType })
            .ToListAsync(cancellationToken);

        // ── Bills ──
        var billIdsSharedByMe = sharedByMeIds
            .Where(s => s.EntityType == EntityTypes.Bill).Select(s => s.EntityId);
        var billIdsSharedWithMe = sharedWithMeIds
            .Where(s => s.EntityType == EntityTypes.Bill).Select(s => s.EntityId);
        var billIdsSplitedWithMe = await dbContext.BillSplits
            .Where(s => s.UserId == currentUserId).Select(s => s.BillId).ToListAsync(cancellationToken);

        var sharedBills = await dbContext.Bills
            .AsNoTracking()
            .Where(b => !b.IsDeleted)
            .Where(b => billIdsSharedByMe.Contains(b.Id) || billIdsSharedWithMe.Contains(b.Id) || billIdsSplitedWithMe.Contains(b.Id))
            .OrderByDescending(b => b.BillDate)
            .Select(b => new BillBriefDto
            {
                Id = b.Id,
                Title = b.Title,
                Amount = b.Amount,
                Currency = b.Currency,
                Category = b.Category,
                BillDate = b.BillDate,
                PaidByUserId = b.PaidByUserId,
                HasReceipt = b.ReceiptUrl != null,
                SplitCount = b.Splits.Count,
                IsFullyPaid = b.Splits.Count == 0 || b.Splits.All(s => s.Status == SplitStatus.Paid || s.Status == SplitStatus.Settled),
                HasLinkedTask = b.RelatedEntityId != null && b.RelatedEntityType != null,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Enrich bill PaidByUserFullName
        var paidByUserIds = sharedBills
            .Select(b => b.PaidByUserId)
            .Distinct()
            .ToList();

        if (paidByUserIds.Count > 0)
        {
            var nameMap = await identityService.GetUserFullNamesByIdsAsync(paidByUserIds, cancellationToken);
            for (var i = 0; i < sharedBills.Count; i++)
            {
                sharedBills[i] = sharedBills[i] with
                {
                    PaidByUserFullName = nameMap.GetValueOrDefault(sharedBills[i].PaidByUserId)
                };
            }
        }

        // ── Tasks ──
        var taskIdsSharedByMe = sharedByMeIds
            .Where(s => s.EntityType == EntityTypes.HouseholdTask).Select(s => s.EntityId);
        var taskIdsSharedWithMe = sharedWithMeIds
            .Where(s => s.EntityType == EntityTypes.HouseholdTask).Select(s => s.EntityId);

        var sharedTasks = await dbContext.HouseholdTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .Where(t => taskIdsSharedByMe.Contains(t.Id) || taskIdsSharedWithMe.Contains(t.Id))
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new SharedTaskBriefDto
            {
                Id = t.Id,
                Title = t.Title,
                Category = t.Category.ToString(),
                Priority = t.Priority.ToString(),
                IsRecurring = t.IsRecurring,
                IsActive = t.IsActive,
                NextDueDate = t.DueDate,
                OccurrenceCount = t.Occurrences.Count(o => !o.IsDeleted)
            })
            .ToListAsync(cancellationToken);

        // ── Budgets ──
        var budgetIdsSharedByMe = sharedByMeIds
            .Where(s => s.EntityType == EntityTypes.Budget).Select(s => s.EntityId);
        var budgetIdsSharedWithMe = sharedWithMeIds
            .Where(s => s.EntityType == EntityTypes.Budget).Select(s => s.EntityId);

        var sharedBudgets = await dbContext.Budgets
            .AsNoTracking()
            .Where(b => !b.IsDeleted)
            .Where(b => budgetIdsSharedByMe.Contains(b.Id) || budgetIdsSharedWithMe.Contains(b.Id))
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BudgetBriefDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                Amount = b.Amount,
                Currency = b.Currency,
                Category = b.Category,
                Period = b.Period,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                IsRecurring = b.IsRecurring,
                IsShared = true,
                ParentBudgetId = b.ParentBudgetId,
                ParentBudgetName = b.ParentBudget != null ? b.ParentBudget.Name : null,
                ChildBudgetCount = b.ChildBudgets.Count(c => !c.IsDeleted),
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // ── Shopping Lists ──
        var listIdsSharedByMe = sharedByMeIds
            .Where(s => s.EntityType == EntityTypes.ShoppingList).Select(s => s.EntityId);
        var listIdsSharedWithMe = sharedWithMeIds
            .Where(s => s.EntityType == EntityTypes.ShoppingList).Select(s => s.EntityId);

        var sharedLists = await dbContext.ShoppingLists
            .AsNoTracking()
            .Where(sl => !sl.IsDeleted)
            .Where(sl => listIdsSharedByMe.Contains(sl.Id) || listIdsSharedWithMe.Contains(sl.Id))
            .OrderByDescending(sl => sl.CreatedAt)
            .Select(sl => new ShoppingListBriefDto
            {
                Id = sl.Id,
                Title = sl.Title,
                Category = sl.Category,
                DueDate = sl.DueDate,
                IsCompleted = sl.IsCompleted,
                TotalItems = sl.Items.Count,
                CheckedItems = sl.Items.Count(i => i.IsChecked),
                CreatedAt = sl.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Count total occurrences across all shared tasks
        var sharedTaskIds = sharedTasks.Select(t => t.Id).ToList();
        var totalOccurrences = sharedTaskIds.Count > 0
            ? await dbContext.TaskOccurrences
                .AsNoTracking()
                .Where(o => !o.IsDeleted && sharedTaskIds.Contains(o.HouseholdTaskId))
                .CountAsync(cancellationToken)
            : 0;

        return new SharedHistoryDto
        {
            UserId = targetUserId,
            UserFullName = $"{targetUser.FirstName} {targetUser.LastName}",
            UserAvatarUrl = targetUser.AvatarUrl,
            ConnectedSince = connection?.RespondedAt ?? connection?.CreatedAt,
            SharedBillCount = sharedBills.Count,
            SharedBudgetCount = sharedBudgets.Count,
            SharedTaskCount = sharedTasks.Count,
            SharedTaskOccurrenceCount = totalOccurrences,
            SharedShoppingListCount = sharedLists.Count,
            SharedBills = sharedBills,
            SharedBudgets = sharedBudgets,
            SharedTasks = sharedTasks,
            SharedShoppingLists = sharedLists
        };
    }
}
