using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.ShoppingLists.Queries.GroupShoppingItems;

public sealed class GroupShoppingItemsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShoppingItemGroupingService groupingService)
    : IRequestHandler<GroupShoppingItemsQuery, ShoppingItemGroupResult>
{
    public async Task<ShoppingItemGroupResult> Handle(
        GroupShoppingItemsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var shoppingList = await dbContext.ShoppingLists
            .AsNoTracking()
            .Include(sl => sl.Items)
            .Where(sl => !sl.IsDeleted)
            .FirstOrDefaultAsync(sl => sl.Id == request.ShoppingListId, cancellationToken)
            ?? throw new NotFoundException(nameof(ShoppingList), request.ShoppingListId);

        var isOwner = shoppingList.CreatedBy == userId;
        var isShared = !isOwner && await dbContext.EntityShares
            .AnyAsync(s => s.EntityType == EntityTypes.ShoppingList
                && s.EntityId == shoppingList.Id
                && s.SharedWithUserId == userId
                && !s.IsDeleted, cancellationToken);

        if (!isOwner && !isShared)
            throw new ForbiddenAccessException();

        var uncheckedNames = shoppingList.Items
            .Where(i => !i.IsChecked)
            .Select(i => i.Name)
            .ToList();

        if (uncheckedNames.Count == 0)
            return new ShoppingItemGroupResult();

        return await groupingService.GroupItemsAsync(uncheckedNames, cancellationToken);
    }
}
