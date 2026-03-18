using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.ShoppingLists.Common;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.ShoppingLists.Queries.GetShoppingListById;

public sealed class GetShoppingListByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService)
    : IRequestHandler<GetShoppingListByIdQuery, ShoppingListDetailDto>
{
    public async Task<ShoppingListDetailDto> Handle(
        GetShoppingListByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var shoppingList = await dbContext.ShoppingLists
            .AsNoTracking()
            .Include(sl => sl.Items.OrderBy(i => i.SortOrder))
            .Where(sl => !sl.IsDeleted)
            .FirstOrDefaultAsync(sl => sl.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ShoppingList), request.Id);

        var isOwner = shoppingList.CreatedBy == userId;
        var isShared = !isOwner && await dbContext.EntityShares
            .AnyAsync(s => s.EntityType == EntityTypes.ShoppingList
                && s.EntityId == shoppingList.Id
                && s.SharedWithUserId == userId
                && !s.IsDeleted, cancellationToken);

        if (!isOwner && !isShared)
            throw new ForbiddenAccessException();

        // Resolve user full names
        var userIds = shoppingList.Items
            .Where(i => !string.IsNullOrEmpty(i.CheckedByUserId))
            .Select(i => i.CheckedByUserId!)
            .Distinct()
            .ToList();

        if (!string.IsNullOrEmpty(shoppingList.CreatedBy) && !userIds.Contains(shoppingList.CreatedBy))
            userIds.Add(shoppingList.CreatedBy);

        var nameMap = userIds.Count > 0
            ? await identityService.GetUserFullNamesByIdsAsync(userIds, cancellationToken)
            : new Dictionary<string, string>();

        // Compute average unit price per item name from bill items linked to this list
        var avgPriceMap = await dbContext.BillItems
            .AsNoTracking()
            .Where(bi => bi.ShoppingListId == shoppingList.Id)
            .GroupBy(bi => bi.Name.ToLower())
            .Select(g => new { Name = g.Key, AvgUnitPrice = g.Average(bi => bi.UnitPrice) })
            .ToDictionaryAsync(x => x.Name, x => Math.Round(x.AvgUnitPrice, 2), StringComparer.OrdinalIgnoreCase, cancellationToken);

        return new ShoppingListDetailDto
        {
            Id = shoppingList.Id,
            Title = shoppingList.Title,
            Description = shoppingList.Description,
            Category = shoppingList.Category,
            DueDate = shoppingList.DueDate,
            DefaultBudgetId = shoppingList.DefaultBudgetId,
            IsCompleted = shoppingList.IsCompleted,
            CompletedAt = shoppingList.CompletedAt,
            CreatedAt = shoppingList.CreatedAt,
            CreatedBy = shoppingList.CreatedBy,
            CreatedByFullName = shoppingList.CreatedBy is not null
                ? nameMap.GetValueOrDefault(shoppingList.CreatedBy)
                : null,
            LastModifiedAt = shoppingList.LastModifiedAt,
            Items = shoppingList.Items.Select(i => new ShoppingItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Notes = i.Notes,
                IsChecked = i.IsChecked,
                CheckedAt = i.CheckedAt,
                CheckedByUserId = i.CheckedByUserId,
                CheckedByUserFullName = i.CheckedByUserId is not null
                    ? nameMap.GetValueOrDefault(i.CheckedByUserId)
                    : null,
                SortOrder = i.SortOrder,
                AveragePrice = avgPriceMap.GetValueOrDefault(i.Name)
            }).ToList()
        };
    }
}
