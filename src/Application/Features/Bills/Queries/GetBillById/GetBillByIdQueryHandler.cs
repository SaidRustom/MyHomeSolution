using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Common;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Bills.Queries.GetBillById;

public sealed class GetBillByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService)
    : IRequestHandler<GetBillByIdQuery, BillDetailDto>
{
    public async Task<BillDetailDto> Handle(GetBillByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var bill = await dbContext.Bills
            .AsNoTracking()
            .Include(b => b.Splits)
            .Include(b => b.Items)
            .Where(b => !b.IsDeleted)
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Bill), request.Id);

        var isOwner = bill.CreatedBy == userId;
        var hasSplitAccess = bill.Splits.Any(s => s.UserId == userId);
        var hasShareAccess = await dbContext.EntityShares
            .AnyAsync(s => s.EntityType == EntityTypes.Bill
                && s.EntityId == bill.Id
                && s.SharedWithUserId == userId
                && !s.IsDeleted, cancellationToken);

        if (!isOwner && !hasSplitAccess && !hasShareAccess)
            throw new ForbiddenAccessException();

        var allUserIds = bill.Splits.Select(s => s.UserId)
            .Concat(bill.Splits.Where(s => s.OwedToUserId is not null).Select(s => s.OwedToUserId!))
            .Append(bill.PaidByUserId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        if (!string.IsNullOrEmpty(bill.CreatedBy) && !allUserIds.Contains(bill.CreatedBy))
            allUserIds.Add(bill.CreatedBy);

        var nameMap = await identityService.GetUserFullNamesByIdsAsync(allUserIds, cancellationToken);

        // Resolve avatars for all users
        var avatarMap = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var uid in allUserIds)
        {
            var detail = await identityService.GetUserByIdAsync(uid, cancellationToken);
            avatarMap[uid] = detail?.AvatarUrl;
        }

        // Resolve related task information
        string? relatedTaskName = null;
        Guid? relatedTaskId = null;
        Guid? relatedOccurrenceId = null;

        if (bill.RelatedEntityId.HasValue)
        {
            if (string.Equals(bill.RelatedEntityType, "TaskOccurrence", StringComparison.OrdinalIgnoreCase))
            {
                var occ = await dbContext.TaskOccurrences
                    .AsNoTracking()
                    .Include(o => o.HouseholdTask)
                    .FirstOrDefaultAsync(o => o.Id == bill.RelatedEntityId.Value, cancellationToken);

                if (occ is not null)
                {
                    relatedTaskName = occ.HouseholdTask.Title;
                    relatedTaskId = occ.HouseholdTaskId;
                    relatedOccurrenceId = occ.Id;
                }
            }
            else if (string.Equals(bill.RelatedEntityType, "HouseholdTask", StringComparison.OrdinalIgnoreCase))
            {
                var task = await dbContext.HouseholdTasks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == bill.RelatedEntityId.Value, cancellationToken);

                if (task is not null)
                {
                    relatedTaskName = task.Title;
                    relatedTaskId = task.Id;
                }
            }
        }

        return new BillDetailDto
        {
            Id = bill.Id,
            Title = bill.Title,
            Description = bill.Description,
            Amount = bill.Amount,
            Currency = bill.Currency,
            Category = bill.Category,
            BillDate = bill.BillDate,
            PaidByUserId = bill.PaidByUserId,
            PaidByUserFullName = nameMap.GetValueOrDefault(bill.PaidByUserId),
            PaidByUserAvatarUrl = avatarMap.GetValueOrDefault(bill.PaidByUserId),
            ReceiptUrl = bill.ReceiptUrl,
            RelatedEntityId = bill.RelatedEntityId,
            RelatedEntityType = bill.RelatedEntityType,
            RelatedTaskName = relatedTaskName,
            RelatedTaskId = relatedTaskId,
            RelatedOccurrenceId = relatedOccurrenceId,
            Notes = bill.Notes,
            CreatedAt = bill.CreatedAt,
            CreatedByUserId = bill.CreatedBy,
            CreatedBy = bill.CreatedBy == null ? null : nameMap.GetValueOrDefault(bill.CreatedBy),
            CreatedByAvatarUrl = bill.CreatedBy == null ? null : avatarMap.GetValueOrDefault(bill.CreatedBy),
            LastModifiedAt = bill.LastModifiedAt,
            Splits = bill.Splits.Select(s => new BillSplitDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserFullName = nameMap.GetValueOrDefault(s.UserId),
                UserAvatarUrl = avatarMap.GetValueOrDefault(s.UserId),
                Percentage = s.Percentage,
                Amount = s.Amount,
                Status = s.Status,
                PaidAt = s.PaidAt,
                OwedToUserId = s.OwedToUserId,
                OwedToUserFullName = s.OwedToUserId is not null
                    ? nameMap.GetValueOrDefault(s.OwedToUserId)
                    : null
            }).ToList(),
            Items = bill.Items.Select(i => new BillItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Price = i.Price,
                Discount = i.Discount
            }).ToList()
        };
    }
}
